namespace Rosetta.Server
{
    using System.Diagnostics;
    using System.IO.Pipes;
    using System.Threading.Tasks;

    internal static class Program
    {
        public static async Task Main()
        {
            var stdInPipeName = @"input";
            var stdOutPipeName = @"output";

            var pipeAccessRule = new PipeAccessRule("Everyone", PipeAccessRights.ReadWrite, System.Security.AccessControl.AccessControlType.Allow);
            var pipeSecurity = new PipeSecurity();
            pipeSecurity.AddAccessRule(pipeAccessRule);

            var readerPipe = new NamedPipeClientStream(stdInPipeName);
            var writerPipe = new NamedPipeClientStream(stdOutPipeName);

            await readerPipe.ConnectAsync();
            await writerPipe.ConnectAsync();

            await LanguageServer.RunServerAsync(
                readerPipe,
                writerPipe);
        }
    }
}