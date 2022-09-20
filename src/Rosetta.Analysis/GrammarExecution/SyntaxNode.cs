namespace Rosetta.Analysis.GrammarExecution
{
    using System;
    using System.Collections.Generic;
    using Rosetta.Analysis.Text;

    public sealed class SyntaxNode
    {
        public SyntaxNode(
            SnapshotSegment text,
            string ruleName,
            IReadOnlyList<SyntaxNode> children = null)
        {
            this.Text = text;
            this.RuleName = ruleName ?? throw new ArgumentNullException(nameof(ruleName));
            this.Children = children ?? Array.Empty<SyntaxNode>();
        }

        public SnapshotSegment Text { get; }

        public string RuleName { get; }

        public IReadOnlyList<SyntaxNode> Children { get; }
    }
}
