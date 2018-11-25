using System;
using System.Linq;
using Hangfire.Console.Runtime;
using Hangfire.States;
using Hangfire.Storage;
using Hangfire.Console.Serialization;
using Hangfire.Console.Server;
using Hangfire.Console.Storage;
using Hangfire.Console.Storage.Operations;
using Hangfire.Console.Utils;

namespace Hangfire.Console.States
{
    internal class ConsoleApplyStateFilter : IApplyStateFilter
    {
        private readonly ConsoleOptions _options;

        public ConsoleApplyStateFilter(ConsoleOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }
        
        public void OnStateApplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
        {
            if (!_options.FollowJobRetentionPolicy)
            {
                // Console sessions use their own expiration timeout.
                // Do not expire here, will be expired by ConsoleServerFilter.
                return;
            }
            
            if (!context.Connection.IsSupported())
            {
                // Unsupported job storage provider
                return;
            }

            var details = context.Storage.GetMonitoringApi().JobDetails(context.BackgroundJob.Id);
            if (details?.History == null)
            {
                // WTF?!
                return;
            }

            if (!ConsoleCentral.TryGetCentral(context.Storage, out var central))
                central = null;
            
            foreach (var state in details.History.Where(x => x.IsProcessingState()))
            {
                if (!ConsoleId.TryCreate(context.BackgroundJob.Id, state.Data, out var consoleId)) continue;

                Operation operation;
                
                if (context.NewState.IsFinal)
                {
                    // Job in final state is a subject for expiration.
                    // All console sessions should also be expired along with it.
                    operation = new ExpireOperation(consoleId, context.JobExpirationTimeout);
                }
                else
                {
                    // Job will be persisted, so should all its console sessions.
                    operation = new PersistOperation(consoleId);
                }
                
                operation.Apply((JobStorageTransaction)transaction);
                
                // also push operation to Central, if available
                // so all future writes are correctly expired/persisted
                central?.Write(operation);
            }
        }

        public void OnStateUnapplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
        {
        }
    }
}
