using System;
using Hangfire.Console.Runtime;
using Hangfire.Console.Serialization;
using Hangfire.Console.Storage;
using Hangfire.Server;

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
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public void OnPerforming(PerformingContext context)
        {
            if (!context.Connection.IsSupported())
            {
                // Unsupported job storage provider
                return;
            }
            
            var state = context.Connection.GetStateData(context.BackgroundJob.Id);

            if (!ConsoleId.TryCreate(context.BackgroundJob.Id, state, out var consoleId))
            {
                // Not in Processing state
                return;
            }

            IOperationStream stream = null;
            
            if (state.TryGetCentral(out var central))
            {
                // open an asynchronous background write stream
                stream = central.CreateConsoleStream(consoleId);
            }
            
            var storage = new ConsoleStorage(context.Connection, stream);
            
            context.Items[ConsoleContext.Key] = new ConsoleContext(consoleId, storage);
        }

        public void OnPerformed(PerformedContext context)
        {
            var console = ConsoleContext.FromPerformContext(context);
            if (console == null)
            {
                // Console was not initialized in OnPerforming
                return;
            }
            
            console.Flush();

            if (_options.FollowJobRetentionPolicy)
            {
                // Console sessions follow parent job expiration.
                // Normally, ConsoleApplyStateFilter will update expiration for all consoles, no extra work needed.

                // If the running job is deleted from the Dashboard, ConsoleApplyStateFilter will be called immediately,
                // but the job will continue running, unless IJobCancellationToken.ThrowIfCancellationRequested() is met. 
                // If anything is written to console after the job was deleted, it won't get a correct expiration assigned.

                // Need to re-apply expiration to prevent those records from becoming eternal garbage.
                console.FixExpiration();
            }
            else
            {
                console.Expire(_options.ExpireIn);
            }

            // remove console context to prevent further writes from filters
            context.Items.Remove(ConsoleContext.Key);
        }
    }
}
