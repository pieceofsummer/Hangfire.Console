using Hangfire.Common;
using Hangfire.Console.Serialization;
using Hangfire.Console.Storage;
using Hangfire.Server;
using Hangfire.States;
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

            if (state == null)
            {
                // State for job not found?
                return;
            }

            if (!string.Equals(state.Name, ProcessingState.StateName, StringComparison.OrdinalIgnoreCase))
            {
                // Not in Processing state? Something is really off...
                return;
            }
            
            var startedAt = JobHelper.DeserializeDateTime(state.Data["StartedAt"]);

            context.Items["ConsoleContext"] = new ConsoleContext(
                new ConsoleId(context.BackgroundJob.Id, startedAt),
                new ConsoleStorage(context.Connection));
        }

        public void OnPerformed(PerformedContext context)
        {
            if (context.Canceled)
            {
                // Processing was been cancelled by one of the job filters
                // There's nothing to do here, as processing hasn't started
                return;
            }

            ConsoleContext.FromPerformContext(context)?.Expire(_options.ExpireIn);
        }
    }
}
