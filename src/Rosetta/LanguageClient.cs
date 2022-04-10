namespace Rosetta.VSExtension
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Diagnostics;
    using System.IO;
    using System.IO.Pipes;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.LanguageServer.Client;
    using Microsoft.VisualStudio.Threading;
    using Microsoft.VisualStudio.Utilities;

    [Export(typeof(ILanguageClient))]
    [ContentType(ContentType.Name)]
    internal sealed class LanguageClient : ILanguageClient
    {
        public string Name { get; } = "Rosetta language server";

        public IEnumerable<string> ConfigurationSections { get; } = null;

        public object InitializationOptions { get; } = null;

        public IEnumerable<string> FilesToWatch { get; } = null;

        public bool ShowNotificationOnInitializeFailed { get; } = true;

        public event AsyncEventHandler<EventArgs> StartAsync;
        public event AsyncEventHandler<EventArgs> StopAsync;

        public async Task<Connection> ActivateAsync(CancellationToken token)
        {
            ProcessStartInfo info = new ProcessStartInfo();
            var programPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"Rosetta.Server.exe");
            info.FileName = programPath;
            info.WorkingDirectory = Path.GetDirectoryName(programPath);

            var stdInPipeName = @"output";
            var stdOutPipeName = @"input";

            var pipeAccessRule = new PipeAccessRule("Everyone", PipeAccessRights.ReadWrite, System.Security.AccessControl.AccessControlType.Allow);
            var pipeSecurity = new PipeSecurity();
            pipeSecurity.AddAccessRule(pipeAccessRule);

            var bufferSize = 256;
            var readerPipe = new NamedPipeServerStream(stdInPipeName, PipeDirection.InOut, 4, PipeTransmissionMode.Message, PipeOptions.Asynchronous, bufferSize, bufferSize, pipeSecurity);
            var writerPipe = new NamedPipeServerStream(stdOutPipeName, PipeDirection.InOut, 4, PipeTransmissionMode.Message, PipeOptions.Asynchronous, bufferSize, bufferSize, pipeSecurity);

            Process process = new Process();
            process.StartInfo = info;

            if (process.Start())
            {
                await readerPipe.WaitForConnectionAsync(token);
                await writerPipe.WaitForConnectionAsync(token);

                return new Connection(readerPipe, writerPipe);
            }

            return null;
        }

        public async Task OnLoadedAsync()
        {
            await this.StartAsync.InvokeAsync(this, EventArgs.Empty);
        }

        public Task OnServerInitializedAsync() => Task.CompletedTask;

        public Task<InitializationFailureContext> OnServerInitializeFailedAsync(ILanguageClientInitializationInfo initializationState)
        {
            return Task.FromResult(new InitializationFailureContext()
            {
                FailureMessage = "Failed to start Rosetta language server",
            });
        }
    }
}
