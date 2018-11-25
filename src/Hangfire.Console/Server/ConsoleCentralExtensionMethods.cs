using Hangfire.Console.Utils;
using Hangfire.Server;
using Hangfire.Storage;

namespace Hangfire.Console.Server
{
    internal static class ConsoleCentralExtensionMethods
    {
        public static IConsoleCentralWorker GetCentralWorker(this BackgroundProcessContext context)
        {
            return ConsoleCentral.GetWorker(context);
        }

        public static bool TryGetCentral(this StateData state, out IConsoleCentralClient central)
        {
            central = null;
            return state.IsProcessingState() &&
                   state.Data.TryGetValue("ServerId", out var serverId) &&
                   ConsoleCentral.TryGetCentral(serverId, out central);
        }
    }
}