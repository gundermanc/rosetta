namespace Rosetta.Server
{
    using System.IO;
    using System.Threading.Tasks;
    using System;
    using StreamJsonRpc;
    using Microsoft.VisualStudio.LanguageServer.Protocol;

    internal sealed class LanguageServer
    {
        private readonly JsonRpc rpc;

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
                        Change = TextDocumentSyncKind.Full,
                    },
                },
            };
        }

        [JsonRpcMethod(Methods.TextDocumentDidOpenName, UseSingleObjectParameterDeserialization = true)]
        public void OnTextDocumentOpened(DidOpenTextDocumentParams arg)
        {
            throw new NotImplementedException();
        }

        [JsonRpcMethod(Methods.TextDocumentDidCloseName, UseSingleObjectParameterDeserialization = true)]
        public void OnTextDocumentClosed(DidCloseTextDocumentParams arg)
        {
            throw new NotImplementedException();
        }

        [JsonRpcMethod(Methods.TextDocumentDidChangeName, UseSingleObjectParameterDeserialization = true)]
        public void OnTextDocumentChanged(DidChangeTextDocumentParams arg)
        {
            throw new NotImplementedException();
        }
    }
}
