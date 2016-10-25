using Hangfire.Common;
using Hangfire.Console.Serialization;
using Hangfire.Server;
using Hangfire.States;
using Hangfire.Storage;
using System;

namespace Hangfire.Console.Server
{
    /// <summary>
    /// Server filter to initialize and cleanup console environment.
    /// </summary>
    internal class ConsoleServerFilter : IServerFilter
    {
        private readonly ConsoleOptions _options;

        public ConsoleServerFilter(ConsoleOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            _options = options;
        }

        public void OnPerforming(PerformingContext context)
        {
            var state = context.Connection.GetStateData(context.BackgroundJob.Id);

            if (!string.Equals(state.Name, ProcessingState.StateName, StringComparison.OrdinalIgnoreCase))
            {
                // Not in Processing state? Something is really off...
                return;
            }
            
            var startedAt = JobHelper.DeserializeDateTime(state.Data["StartedAt"]);

            context.Items["ConsoleId"] = new ConsoleId(context.BackgroundJob.Id, startedAt);
        }

        public void OnPerformed(PerformedContext context)
        {
            if (context.Canceled)
            {
                // Processing was been cancelled by one of the job filters
                // There's nothing to do here, as processing hasn't started
                return;
            }

            if (!context.Items.ContainsKey("ConsoleId"))
            {
                // Something gone wrong in OnPerforming
                return;
            }

            var consoleId = (ConsoleId)context.Items["ConsoleId"];

            using (var tran = (JobStorageTransaction)context.Connection.CreateWriteTransaction())
            {
                tran.ExpireSet(consoleId.ToString(), _options.ExpireIn);
                tran.Commit();
            }
        }
    }
}
