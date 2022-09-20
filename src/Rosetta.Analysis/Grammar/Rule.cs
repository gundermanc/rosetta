namespace Rosetta.Analysis.Grammar
{
    using System.Collections.Generic;
    using System.IO;

    public abstract class Rule
    {
        public Rule(RuleType type)
        {
            RuleType = type;
        }

        public RuleType RuleType { get; }
    }

    public sealed class MatchRule : Rule
    {
        public MatchRule(string matchText)
            : base(RuleType.Match)
        {
            MatchText = matchText;
        }

        public string MatchText { get; }
    }

    public sealed class ReferenceRule : Rule
    {
        private readonly IReadOnlyDictionary<string, Rule> rules;

        public ReferenceRule(string ruleName, IReadOnlyDictionary<string, Rule> rules)
            : base(RuleType.ReferenceRule)
        {
            this.RuleName = ruleName;
            this.rules = rules;
        }

        public string RuleName { get; }

        public Rule ConcreteRule
        {
            get
            {
                if (!this.rules.TryGetValue(this.RuleName, out var concreteRule))
                {
                    throw new InvalidDataException("Cannot resolve rule reference");
                }

                return concreteRule;
            }
        }
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
