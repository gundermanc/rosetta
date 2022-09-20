namespace Rosetta.Analysis.GrammarExecution
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
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

            if (matchRule.Regex is not null)
            {
                return ExecuteRegexMatchRule(textSnapshot, matchRule, parentName, ref i);
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

        private static SyntaxNode? ExecuteRegexMatchRule(
            SnapshotBase textSnapshot,
            MatchRule matchRule,
            string? parentName,
            ref int i)
        {
            int localI = i;

            // HACK: we need a way to turn whitespace significance on/off.
            //       the ideal way is via a dedicated command that takes a
            //       regex of chars to ignore.
            //       For now I'll just use _ prefix.
            if (parentName?.StartsWith("_") is false)
            {
                for (; localI < textSnapshot.Length - 1 && char.IsWhiteSpace(textSnapshot[localI]); localI++) ;
            }

            var startPosition = localI;

            // TODO: copying the entire text buffer to a string is EXTRAORDINARILY expensive.
            //       Up until net7.0 TFM, .NET regex library only supported running matches on
            //       string inputs: https://devblogs.microsoft.com/dotnet/regular-expression-improvements-in-dotnet-7/#spans.
            //
            //       We should switch this project to net7.0 to take advantage of Span<char> support
            //       once the VSIX project is updated to allow builds without mismatched-TFM errors.
            //
            //       NOTE: this is not a panacea, we will still end up having to do a memory copy at
            //             least... Span<T> can only operate on contiguous spans of memory, which
            //             SnapshotBase may not be.
            var match = matchRule.Regex.Match(textSnapshot.ToStringCached(), localI, textSnapshot.Length - localI);

            if (!match.Success)
            {
                return null;
            }

            localI += match.Length;

            var length = localI - startPosition;

            i = localI;

            return new SyntaxNode(
                new SnapshotSegment(textSnapshot, startPosition, length),
                parentName ?? "ANONYMOUS_MATCH_RULE");
        }
    }
}
