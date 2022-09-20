namespace Rosetta.Server
{
    using System;
    using Microsoft.VisualStudio.Text;
    using Rosetta.Analysis.Text;

    internal sealed class TextSnapshot : SnapshotBase
    {
        public TextSnapshot(ITextSnapshot snapshot) : base(snapshot.Length)
        {
            this.Snapshot = snapshot
                ?? throw new ArgumentNullException(nameof(snapshot));
        }

        public override char this[int offset] => this.Snapshot[offset];

        public ITextSnapshot Snapshot { get; }
    }
}