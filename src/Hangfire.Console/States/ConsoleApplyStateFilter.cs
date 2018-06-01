using Hangfire.States;
using System;
using System.Linq;
using Hangfire.Storage;
using Hangfire.Console.Serialization;
using Hangfire.Common;
using Hangfire.Console.Storage;

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

            var jobDetails = context.Storage.GetMonitoringApi().JobDetails(context.BackgroundJob.Id);
            if (jobDetails == null || jobDetails.History == null)
            {
                // WTF?!
                return;
            }

            var expiration = new ConsoleExpirationTransaction((JobStorageTransaction)transaction);

            foreach (var state in jobDetails.History.Where(x => x.StateName == ProcessingState.StateName))
            {
                var consoleId = new ConsoleId(context.BackgroundJob.Id, JobHelper.DeserializeDateTime(state.Data["StartedAt"]));

                if (context.NewState.IsFinal)
                {
                    // Job in final state is a subject for expiration.
                    // To keep storage clean, its console sessions should also be expired.
                    expiration.Expire(consoleId, context.JobExpirationTimeout);
                }
                else
                {
                    // Job will be persisted, so should its console sessions.
                    expiration.Persist(consoleId);
                }
            }
        }

        public void OnStateUnapplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
        {
        }
    }
}
