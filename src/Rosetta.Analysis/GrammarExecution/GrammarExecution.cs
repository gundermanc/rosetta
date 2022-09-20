namespace Rosetta.Analysis.GrammarExecution
{
    using System;
    using System.Collections.Generic;
    using Rosetta.Analysis.Grammar;
    using Rosetta.Analysis.Text;

    public sealed class GrammarExecution
    {
        public static SyntaxTree Parse(Grammar grammar, SnapshotBase textSnapshot)
        {
            int i = 0;
            var rootNode = ExecuteAnyRule(textSnapshot, grammar.Root, "ROOT", ref i);

            return new SyntaxTree(rootNode ?? new SyntaxNode(textSnapshot.Extent, "ROOT"));
        }

        private static SyntaxNode? ExecuteAnyRule(
            SnapshotBase textSnapshot,
            Rule rule,
            string? parentName,
            ref int i)
        {
            switch (rule.RuleType)
            {
                case RuleType.Match:
                    return ExecuteMatchRule(textSnapshot, rule, parentName, ref i);

                case RuleType.AndRule:
                    return ExecuteAndRule(textSnapshot, rule, parentName, ref i);

                case RuleType.OrRule:
                    return ExecuteOrRule(textSnapshot, rule, parentName, ref i);

                case RuleType.ReferenceRule:
                    return rule is ReferenceRule refRule ?
                        ExecuteAnyRule(textSnapshot, refRule.ConcreteRule, refRule.RuleName, ref i) :
                        throw new InvalidOperationException("Inconsistent rule type");

                default:
                    throw new NotImplementedException();
            }
        }

        private static SyntaxNode? ExecuteAndRule(
            SnapshotBase textSnapshot,
            Rule rule,
            string? parentName,
            ref int i)
        {
            if (rule is not AndRule andRule)
            {
                throw new InvalidOperationException("Inconsistent rule type");
            }

            var children = new List<SyntaxNode>();

            var startPosition = i;

            // Loop over all child rules.
            foreach (var childRule in andRule.Children)
            {
                SyntaxNode? node = ExecuteAnyRule(textSnapshot, childRule, parentName, ref i);

                // Bail if we're missing a required part.
                if (node is null)
                {
                    return null;
                }

                children.Add(node);
            }

            return new SyntaxNode(
                new SnapshotSegment(textSnapshot, startPosition, i - startPosition),
                parentName ?? "ANONYMOUS_AND_RULE",
                children);
        }

        private static SyntaxNode? ExecuteOrRule(
            SnapshotBase textSnapshot,
            Rule rule,
            string? parentName,
            ref int i)
        {
            if (rule is not OrRule orRule)
            {
                throw new InvalidOperationException("Inconsistent rule type");
            }

            int startPosition = i;

            // Loop until we find a node that matches.
            foreach (var childRule in orRule.Children)
            {
                SyntaxNode? node = ExecuteAnyRule(textSnapshot, childRule, parentName, ref i);

                if (node is not null)
                {
                    return node;
                }

                // Revert any consumed text.
                i = startPosition;
            }

            return null;
        }

        private static SyntaxNode? ExecuteMatchRule(
            SnapshotBase textSnapshot,
            Rule rule,
            string? parentName,
            ref int i)
        {
            if (rule is not MatchRule matchRule)
            {
                throw new InvalidOperationException("Inconsistent rule type");
            }

            if (textSnapshot.Length < matchRule.MatchText.Length)
            {
                return null;
            }

            int localI = i;

            // HACK: we need a way to turn whitespace significance on/off.
            //       the ideal way is via a dedicated command that takes a
            //       regex of chars to ignore.
            //       For now I'll just use _ prefix.
            if (parentName?.StartsWith("_") is false)
            {
                for (; localI < textSnapshot.Length - 1 && char.IsWhiteSpace(textSnapshot[localI]); localI++);
            }

            var startPosition = localI;

            for (int j = 0;
                localI < textSnapshot.Length && j < matchRule.MatchText.Length;
                localI++, j++)
            {
                if (textSnapshot[localI] != matchRule.MatchText[j])
                {
                    return null;
                }
            }

            var length = localI - startPosition;
            if (matchRule.MatchText.Length != length)
            {
                return null;
            }

            i = localI;

            return new SyntaxNode(
                new SnapshotSegment(textSnapshot, startPosition, length),
                parentName ?? "ANONYMOUS_MATCH_RULE");
        }
    }
}
