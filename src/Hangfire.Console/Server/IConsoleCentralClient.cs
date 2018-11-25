using Hangfire.Console.Serialization;
using Hangfire.Console.Storage;

namespace Hangfire.Console.Server
{
    internal interface IConsoleCentralClient
    {
        IOperationStream CreateConsoleStream(ConsoleId consoleId);

        void Write(Operation operation);
    }
}