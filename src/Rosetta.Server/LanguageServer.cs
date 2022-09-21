namespace Rosetta.Server
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.LanguageServer.Protocol;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Utilities;
    using Newtonsoft.Json.Linq;
    using Rosetta.Analysis.GrammarExecution;
    using Rosetta.Analysis.Text;
    using StreamJsonRpc;

    internal sealed class LanguageServer
    {
        private readonly MefHostCompositionService mefHost = new();
        private readonly JsonRpc rpc;
        private readonly Dictionary<Uri, ITextBuffer> openDocuments = new();

        public static async Task RunServerAsync(Stream input, Stream output)
        {
            var server = new LanguageServer(input, output);

            // Wait for exit.
            await server.rpc.Completion;
        }

        private LanguageServer(Stream input, Stream output)
        {
            var messageHandler = new HeaderDelimitedMessageHandler(output, input);
            this.rpc = new JsonRpc(messageHandler, this);
            this.rpc.Disconnected += OnRpcDisconnected;

            // Add support for VS-exclusive public protocol extensions.
            VSExtensionUtilities.AddVSExtensionConverters(((JsonMessageFormatter)messageHandler.Formatter).JsonSerializer);

            this.rpc.StartListening();
        }

        private void OnRpcDisconnected(object sender, JsonRpcDisconnectedEventArgs e)
        {
            this.rpc.Dispose();
        }

        [JsonRpcMethod(Methods.InitializeName, UseSingleObjectParameterDeserialization = true)]
        public InitializeResult Initialize(InitializeParams arg)
        {
            return new InitializeResult()
            {
                Capabilities = new VSServerCapabilities()
                {
                    TextDocumentSync = new TextDocumentSyncOptions()
                    {
                        OpenClose = true,
                        Change = TextDocumentSyncKind.Incremental,
                    },
                    FoldingRangeProvider = true,
                    SemanticTokensOptions = new SemanticTokensOptions()
                    {
                        Range = true,
                        Legend = new SemanticTokensLegend
                        {
                            TokenTypes = SemanticTokenTypes.AllTypes.ToArray(),
                            TokenModifiers = SemanticTokenModifiers.AllModifiers.ToArray()
                        },
                    },
                    WorkspaceSymbolProvider = true
                },
            };
        }

        [JsonRpcMethod(Methods.TextDocumentDidOpenName, UseSingleObjectParameterDeserialization = true)]
        public void OnTextDocumentOpened(DidOpenTextDocumentParams arg)
        {
            lock (this.openDocuments)
            {
                if (this.openDocuments.ContainsKey(arg.TextDocument.Uri))
                {
                    throw new InvalidOperationException("Document is already open");
                }

                var buffer = this.BufferFactoryService.CreateTextBuffer();

                buffer.Insert(0, arg.TextDocument.Text);

                this.openDocuments.Add(
                    arg.TextDocument.Uri,
                    buffer);
            }
        }

        [JsonRpcMethod(Methods.TextDocumentDidCloseName, UseSingleObjectParameterDeserialization = true)]
        public void OnTextDocumentClosed(DidCloseTextDocumentParams arg)
        {
            lock (this.openDocuments)
            {
                if (!this.openDocuments.Remove(arg.TextDocument.Uri))
                {
                    throw new InvalidOperationException("Document is not yet open");
                }
            }
        }

        [JsonRpcMethod(Methods.TextDocumentDidChangeName, UseSingleObjectParameterDeserialization = true)]
        public void OnTextDocumentChanged(DidChangeTextDocumentParams arg)
        {
            ITextSnapshot newSnapshot;

            lock (this.openDocuments)
            {
                if (!this.openDocuments.TryGetValue(arg.TextDocument.Uri, out var buffer))
                {
                    throw new InvalidOperationException("Document is not yet open");
                }

                using (var edit = buffer.CreateEdit())
                {
                    foreach (var change in arg
                        .ContentChanges
                        .OrderByDescending(change => change.Range.Start.Line))
                    {
                        var start = edit.Snapshot.GetLineFromLineNumber(change.Range.Start.Line).Start + change.Range.Start.Character;
                        var end = edit.Snapshot.GetLineFromLineNumber(change.Range.End.Line).Start + change.Range.End.Character;

                        edit.Replace(new SnapshotSpan(start, end), change.Text);
                    }

                    newSnapshot = edit.Apply();
                }
            }

            DebugPrintDocument(newSnapshot);
        }

        [JsonRpcMethod(Methods.TextDocumentFoldingRangeName, UseSingleObjectParameterDeserialization = true)]
        public async Task<FoldingRange[]> GetFoldingRangesAsync(FoldingRangeParams arg)
        {
            (SyntaxTree? parse, TextSnapshot? snapshot) = await this.GetActiveDocumentParseAsync(arg.TextDocument.Uri);

            var builder = ArrayBuilder<FoldingRange>.GetInstance();

            if (parse is not null &&
                snapshot is not null)
            {
                var previousText = new SnapshotSegment(parse.Root.Text.Snapshot, 0, 0);

                this.VisitFoldingRangeNode(
                    parse.Root,
                    builder,
                    (TextSnapshot)parse.Root.Text.Snapshot,
                    ref previousText);
            }

            return builder.ToArrayAndFree();
        }

        [JsonRpcMethod(Methods.TextDocumentSemanticTokensRangeName, UseSingleObjectParameterDeserialization = true)]
        public async Task<SemanticTokens> GetSemanticTokensForRangeAsync(SemanticTokensRangeParams arg)
        {
            (SyntaxTree? parse, TextSnapshot? snapshot) = await this.GetActiveDocumentParseAsync(arg.TextDocument.Uri);

            var builder = ArrayBuilder<int>.GetInstance();

            if (parse is not null &&
                snapshot is not null)
            {
                var previousText = new SnapshotSegment(parse.Root.Text.Snapshot, 0, 0);

                this.VisitSyntaxHighlightNode(
                    parse.Root,
                    builder,
                    (TextSnapshot)parse.Root.Text.Snapshot,
                    ref previousText);
            }

            return new SemanticTokens()
            {
                Data = builder.ToArrayAndFree(),
            };
        }

        [JsonRpcMethod(Methods.WorkspaceSymbolName, UseSingleObjectParameterDeserialization = true)]
        public async Task<SymbolInformation[]> GetWorkspaceSymbolsAsync(WorkspaceSymbolParams arg)
        {
            var responseBuilder = ArrayBuilder<SymbolInformation>.GetInstance();

            List<Uri> openDocuments;
            lock (this.openDocuments)
            {
                openDocuments = this.openDocuments.Keys.ToList();
            }

            // Enumerate open documents and search for matching definitions.
            // TODO: parse and search all supported files in the workspace.
            foreach (var openDocument in openDocuments)
            {
                (SyntaxTree? parse, TextSnapshot? snapshot) = await this.GetActiveDocumentParseAsync(openDocument);

                var previousText = new SnapshotSegment(parse.Root.Text.Snapshot, 0, 0);

                if (parse is not null &&
                    snapshot is not null)
                {
                    VisitSymbolSearchNode(
                        openDocument,
                        arg.Query,
                        parse.Root,
                        responseBuilder,
                        snapshot,
                        ref previousText);
                }
            }

            return responseBuilder.ToArrayAndFree();
        }

        private void VisitFoldingRangeNode(
            SyntaxNode syntaxNode,
            ArrayBuilder<FoldingRange> responseBuilder,
            TextSnapshot snapshot,
            ref SnapshotSegment previousText)
        {
            // Find all type definition names that match the search query and return them.
            if (syntaxNode.RuleName.Contains("BLOCK"))
            {
                var startLine = snapshot.Snapshot.GetLineFromPosition(syntaxNode.Text.Start);
                var endLine = snapshot.Snapshot.GetLineFromPosition(syntaxNode.Text.End);

                responseBuilder.Add(new FoldingRange()
                {
                    StartLine = startLine.LineNumber,
                    EndLine = endLine.LineNumber,
                });
            }

            foreach (var node in syntaxNode.Children)
            {
                VisitFoldingRangeNode(
                    node,
                    responseBuilder,
                    snapshot,
                    ref previousText);
            }
        }

        private void VisitSymbolSearchNode(
            Uri uri,
            string query,
            SyntaxNode syntaxNode,
            ArrayBuilder<SymbolInformation> responseBuilder,
            TextSnapshot snapshot,
            ref SnapshotSegment previousText)
        {
            // Find all type definition names that match the search query and return them.
            if (syntaxNode.RuleName.Contains("DEFINITION") &&
                syntaxNode.Text.GetText().StartsWith(query))
            {
                var startLine = snapshot.Snapshot.GetLineFromPosition(syntaxNode.Text.Start);
                var endLine = snapshot.Snapshot.GetLineFromPosition(syntaxNode.Text.End);

                responseBuilder.Add(new SymbolInformation()
                {
                    Name = syntaxNode.Text.GetText(),
                    Kind = SymbolKind.Function,
                    Location = new Location()
                    {
                        Range = new Range()
                        {
                            Start = new Position(startLine.LineNumber, syntaxNode.Text.Start - startLine.Start),
                            End = new Position(endLine.LineNumber, syntaxNode.Text.End - endLine.Start),
                        },
                        Uri = uri,
                    },
                });
            }

            foreach (var node in syntaxNode.Children)
            {
                VisitSymbolSearchNode(
                    uri,
                    query,
                    node,
                    responseBuilder,
                    snapshot,
                    ref previousText);
            }
        }

        private void VisitSyntaxHighlightNode(
            SyntaxNode syntaxNode,
            ArrayBuilder<int> responseBuilder,
            TextSnapshot snapshot,
            ref SnapshotSegment previousText)
        {
            var type = TypeFromRuleName(syntaxNode.RuleName);

            if (type != -1)
            {
                var thisNodeStartLine = snapshot.Snapshot.GetLineFromPosition(syntaxNode.Text.Start);
                var previousNodeStartLine = snapshot.Snapshot.GetLineFromPosition(previousText.Start);

                // Lines delta from last response.
                responseBuilder.Add(thisNodeStartLine.LineNumber - previousNodeStartLine.LineNumber);

                // Horizontal offset delta from previous response on this line, if any.
                responseBuilder.Add(
                    thisNodeStartLine.LineNumber == previousNodeStartLine.LineNumber ?
                    syntaxNode.Text.Start - previousText.Start :
                    syntaxNode.Text.Start - thisNodeStartLine.Start);

                // Length of token.
                responseBuilder.Add(syntaxNode.Text.Length);

                // Token type.
                responseBuilder.Add(type); // Keyword

                // Token modifier.
                responseBuilder.Add(0);

                previousText = syntaxNode.Text;
            }

            foreach (var node in syntaxNode.Children)
            {
                VisitSyntaxHighlightNode(
                    node,
                    responseBuilder,
                    snapshot,
                    ref previousText);
            }
        }

        private int TypeFromRuleName(string ruleName)
        {
            // TODO: these can be cached and done without a list copy.
            if (ruleName.Contains("NUMBER"))
            {
                return SemanticTokenTypes.AllTypes.ToList().IndexOf(SemanticTokenTypes.String);
            }
            else if (ruleName.Contains("OPERATOR"))
            {
                return SemanticTokenTypes.AllTypes.ToList().IndexOf(SemanticTokenTypes.Operator);
            }
            else if (ruleName.Contains("KEYWORD"))
            {
                return SemanticTokenTypes.AllTypes.ToList().IndexOf(SemanticTokenTypes.Keyword);
            }
            else if (ruleName.Contains("TYPE"))
            {
                return SemanticTokenTypes.AllTypes.ToList().IndexOf(SemanticTokenTypes.Type);
            }
            else if (ruleName.Contains("FUNCTION"))
            {
                return SemanticTokenTypes.AllTypes.ToList().IndexOf(SemanticTokenTypes.Function);
            }
            else
            {
                return -1;
            }
        }

        private async Task<(SyntaxTree?, TextSnapshot?)> GetActiveDocumentParseAsync(Uri uri)
        {
            ITextSnapshot? snapshot;

            lock (this.openDocuments)
            {
                if (!this.openDocuments.TryGetValue(uri, out var buffer))
                {
                    return (null, null);
                }

                snapshot = buffer.CurrentSnapshot;
            }

            var parseManager = await ParseManager.GetOrCreateAsync(snapshot.TextBuffer);
            var parse = parseManager.EnsureParsed(snapshot);

            return (parse, (TextSnapshot?)parse?.Root.Text.Snapshot);
        }

        private ITextBufferFactoryService BufferFactoryService
            => this.mefHost.GetExportedValue<ITextBufferFactoryService>() ??
            throw new InvalidOperationException("Unable to acquire text buffer factory");

        [Conditional("DEBUG")]
        private void DebugPrintDocument(ITextSnapshot snapshot)
        {
            Console.WriteLine($"**************************** v{snapshot.Version.VersionNumber}****************************");

            foreach (var line in snapshot.Lines)
            {
                Console.WriteLine(line.GetText());
            }

            Console.WriteLine($"******************************************************************************************");
        }
    }
}
