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
                new SnapshotSegment(textSnapshot, i, i - startPosition),
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

            // Loop until we find a node that matches.
            foreach (var childRule in orRule.Children)
            {
                SyntaxNode? node = ExecuteAnyRule(textSnapshot, childRule, parentName, ref i);

                if (node is not null)
                {
                    return node;
                }
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

            var startPosition = i;

            for (int j = 0;
                i < textSnapshot.Length && j < matchRule.MatchText.Length;
                i++, j++)
            {
                if (textSnapshot[i] != matchRule.MatchText[j])
                {
                    i = startPosition;
                    return null;
                }
            }

            return new SyntaxNode(
                new SnapshotSegment(textSnapshot, startPosition, i - startPosition),
                parentName ?? "ANONYMOUS_MATCH_RULE");
        }
    }
}
