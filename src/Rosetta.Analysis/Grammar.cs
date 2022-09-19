namespace Rosetta.Analysis
{
    using System;
    using System.Collections.Generic;

    public sealed class Grammar
    {
        internal Grammar(ParentRule root, IReadOnlyDictionary<string, Rule> rules)
        {
            this.Root = root
                ?? throw new ArgumentNullException(nameof(root));

            this.Rules = rules
                ?? throw new ArgumentNullException(nameof(rules));
        }

        public ParentRule Root { get; }

        public IReadOnlyDictionary<string, Rule> Rules { get; }
    }
}
