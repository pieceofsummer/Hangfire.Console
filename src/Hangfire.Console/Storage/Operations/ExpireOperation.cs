using System;
using Hangfire.Console.Serialization;
using Hangfire.Storage;

namespace Hangfire.Console.Storage.Operations
{
    /// <summary>
    /// Expire operation
    /// </summary>
    internal class ExpireOperation : PersistOperation
    {
        /// <inheritdoc />
        public ExpireOperation(ConsoleId consoleId, TimeSpan expireIn) : base(consoleId)
        {
            ExpireIn = expireIn;
        }
        
        /// <summary>
        /// Gets the expiration time.
        /// </summary>
        public TimeSpan ExpireIn { get; }

        /// <inheritdoc />
        public override void Apply(JobStorageTransaction transaction)
        {
            if (transaction == null)
                throw new ArgumentNullException(nameof(transaction));
            
            transaction.ExpireSet(ConsoleId.GetSetKey(), ExpireIn);
            transaction.ExpireHash(ConsoleId.GetHashKey(), ExpireIn);

            // After upgrading to Hangfire.Console version with new keys, 
            // there may be existing background jobs with console attached 
            // to the previous keys. We should expire them also.
            transaction.ExpireSet(ConsoleId.GetOldConsoleKey(), ExpireIn);
            transaction.ExpireHash(ConsoleId.GetOldConsoleKey(), ExpireIn);
        }
    }
}