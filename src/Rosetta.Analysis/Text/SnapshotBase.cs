﻿namespace Rosetta.Analysis.Text
{
    /// <summary>
    /// Abstract representation of an immutable text segment.
    /// </summary>
    public abstract class SnapshotBase
    {
        private string? stringifiedSnapshot;

        protected SnapshotBase(int length)
        {
            this.Length = length;
        }

        /// <summary>
        /// Gets the character located at a specific position in the text segment.
        /// </summary>
        /// <param name="offset"> Position of character in the text segment. </param>
        /// <returns> Character at the specific position. </returns>
        public abstract char this[int offset] { get; }

        public SnapshotSegment Extent => new SnapshotSegment(this, 0, this.Length);

        /// <summary>
        /// Length of the text segment.
        /// </summary>
        public int Length { get; }

        // HACK
        public string ToStringCached() => this.stringifiedSnapshot ??= this.ToString();

        public override string ToString() => this.Extent.GetText();
    }
}