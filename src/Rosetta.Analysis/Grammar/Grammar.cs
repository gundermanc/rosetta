namespace Rosetta.Analysis.Grammar
{
    using System;
    using System.Collections.Generic;

    public sealed class Grammar
    {
        internal Grammar(Rule root, IReadOnlyDictionary<string, Rule> rules)
        {
            Root = root
                ?? throw new ArgumentNullException(nameof(root));

            Rules = rules
                ?? throw new ArgumentNullException(nameof(rules));
        }

        public Rule Root { get; }

        public IReadOnlyDictionary<string, Rule> Rules { get; }
    }
}
