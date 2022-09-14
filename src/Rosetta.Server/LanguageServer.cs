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

                this.openDocuments.Add(arg.TextDocument.Uri, this.BufferFactoryService.CreateTextBuffer());
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
