using Hangfire.Console.Server;
using Hangfire.Server;

namespace Hangfire.Console
{
    /// <summary>
    /// Background process for asynchronous console writes
    /// </summary>
    public class ConsoleBackgroundProcess : IBackgroundProcess
    {
        /// <inheritdoc />
        public void Execute(BackgroundProcessContext context)
        {
            using (var worker = context.GetCentralWorker())
            {
                worker.Execute(context.CancellationToken);
            }
        }
    }
}