namespace Rosetta.Analysis
{
    using System;

    public sealed class Grammar
    {
        internal Grammar(ParentRule root)
        {
            this.Root = root
                ?? throw new ArgumentNullException(nameof(root));
        }

        public ParentRule Root { get; }
    }
}
