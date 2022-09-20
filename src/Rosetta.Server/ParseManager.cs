namespace Rosetta.Server
{
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Threading;
    using Rosetta.Analysis.Grammar;
    using Rosetta.Analysis.GrammarExecution;

    internal sealed class ParseManager
    {
        private readonly object syncRoot = new();
        private ITextSnapshot snapshot;
        private SyntaxTree? syntaxTree;
        private AsyncLazy<Grammar> grammar;

        public static async Task<ParseManager> GetOrCreateAsync(ITextBuffer textBuffer)
        {
            var parseManager = textBuffer.Properties.GetOrCreateSingletonProperty<ParseManager>(() => new ParseManager());

            await parseManager.InitializeAsync();

            return parseManager;
        }

        public SyntaxTree? EnsureParsed(ITextSnapshot textSnapshot)
        {
            lock (this.syncRoot)
            {
                // See if we've already parsed this version.
                if (this.snapshot == textSnapshot)
                {
                    return this.syntaxTree;
                }

                // If we've already parsed a newer version, this is wasted work, bail.
                if (this.snapshot?.Version.VersionNumber > textSnapshot.Version.VersionNumber)
                {
                    return null;
                }

                this.snapshot = textSnapshot;

                // TODO: at some point this lock may end up becoming a bottle neck or source
                // of thread starvation. At that point consider making this async.
                this.syntaxTree = GrammarExecution.Parse(this.grammar.GetValue(), new TextSnapshot(textSnapshot));

                return this.syntaxTree;
            }
        }

        private ParseManager()
        {
#pragma warning disable VSTHRD012 // Provide JoinableTaskFactory where allowed
            this.grammar = new AsyncLazy<Grammar>(() => InitializeAsync());
#pragma warning restore VSTHRD012 // Provide JoinableTaskFactory where allowed
        }

        private async Task<Grammar> InitializeAsync()
        {
            // TODO: include some sort of mechanism for looking up a grammar
            // matching the file extension.
            return await GrammarParser.ParseGrammarAsync(@"D:\Repos\rosetta\samples\CSharp.rosetta.md");
        }
    }
}
