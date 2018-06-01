using Hangfire.Console.Serialization;

namespace Hangfire.Console.Storage
{
    internal static class ConsoleIdExtensions
    {
        public static string GetSetKey(this ConsoleId consoleId) => $"console:{consoleId}";

        public static string GetHashKey(this ConsoleId consoleId) => $"console:refs:{consoleId}";

        public static string GetOldConsoleKey(this ConsoleId consoleId) => consoleId.ToString();
    }
}
