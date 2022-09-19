﻿namespace Rosetta.Analysis
{
    using System.Collections.Generic;

    public abstract class Rule
    {
        public Rule(RuleType type)
        {
            this.RuleType = type;
        }

        public RuleType RuleType { get; }
    }

    public sealed class MatchRule : Rule
    {
        public MatchRule(string matchText)
            : base(RuleType.Match)
        {
            this.MatchText = matchText;
        }

        public string MatchText { get; }
    }

    public sealed class ReferenceRule : Rule
    {
        public ReferenceRule(string ruleName)
            : base(RuleType.ReferenceRule)
        {
            this.RuleName = ruleName;
        }

        public string RuleName { get; }
    }

    public class ParentRule : Rule
    {
        public ParentRule(RuleType type)
            : base(type)
        {
        }

        public List<Rule> Children { get; } = new List<Rule>();
    }

    public sealed class AndRule : ParentRule
    {
        public AndRule() : base(RuleType.AndRule)
        {
        }
    }

    public sealed class OrRule : ParentRule
    {
        public OrRule() : base(RuleType.OrRule)
        {
        }
    }
}
