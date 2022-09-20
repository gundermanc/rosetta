namespace Rosetta.Server
{
    using System.IO;
    using System.Threading.Tasks;
    using System;
    using StreamJsonRpc;
    using Microsoft.VisualStudio.LanguageServer.Protocol;
    using System.Collections.Generic;
    using Microsoft.VisualStudio.Text;
    using System.Linq;
    using System.Diagnostics;
    using Rosetta.Analysis.Grammar;
    using Rosetta.Analysis.Text;
    using Rosetta.Analysis.GrammarExecution;
    using Microsoft.VisualStudio.Utilities;

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
                    SemanticTokensOptions = new SemanticTokensOptions()
                    {
                        Range = true,
                        Legend = new SemanticTokensLegend
                        {
                            TokenTypes = SemanticTokenTypes.AllTypes.ToArray(),
                            TokenModifiers = SemanticTokenModifiers.AllModifiers.ToArray()
                        },
                    }
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

        [JsonRpcMethod(Methods.TextDocumentSemanticTokensRangeName, UseSingleObjectParameterDeserialization = true)]
        public async Task<SemanticTokens> SemanticTokensRangeAsync(SemanticTokensRangeParams arg)
        {
            var grammar = await GrammarParser.ParseGrammarAsync(@"C:\Repos\rosetta\src\Rosetta.Analysis.Tests\Grammars\Arithmetic.rosetta.md");

            TextSnapshot? snapshot;

            lock (this.openDocuments)
            {
                if (!this.openDocuments.TryGetValue(arg.TextDocument.Uri, out var buffer))
                {
                    return new SemanticTokens() { Data = Array.Empty<int>() };
                }

                snapshot = new TextSnapshot(buffer.CurrentSnapshot);
            }

            var parse = GrammarExecution.Parse(grammar, snapshot);

            var builder = ArrayBuilder<int>.GetInstance();

            var previousText = new SnapshotSegment(snapshot, 0, 0);

            this.VisitSyntaxNode(parse.Root, builder, snapshot, ref previousText);

            return new SemanticTokens()
            {
                Data = builder.ToArrayAndFree(),
            };
        }

        private void VisitSyntaxNode(
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
                VisitSyntaxNode(
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
                return SemanticTokenTypes.AllTypes.ToList().IndexOf(SemanticTokenTypes.Keyword);
            }
            else
            {
                return -1;
            }
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
