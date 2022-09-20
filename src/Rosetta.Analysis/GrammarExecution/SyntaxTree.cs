namespace Rosetta.Analysis.GrammarExecution
{
    using System;

    public sealed class SyntaxTree
    {
        public SyntaxTree(SyntaxNode root)
        {
            this.Root = root ?? throw new ArgumentNullException(nameof(root));
        }

        public SyntaxNode Root { get; }
    }
}
