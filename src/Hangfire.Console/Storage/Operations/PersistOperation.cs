using System;
using Hangfire.Console.Serialization;
using Hangfire.Storage;

namespace Hangfire.Console.Storage.Operations
{
    /// <summary>
    /// Persist operation
    /// </summary>
    internal class PersistOperation : Operation
    {
        /// <inheritdoc />
        public PersistOperation(ConsoleId consoleId) : base(consoleId)
        {
            Timestamp = DateTime.UtcNow;
        }
        
        /// <summary>
        /// Returns the object creation time.
        /// </summary>
        internal DateTime Timestamp { get; }

        /// <inheritdoc />
        public override void Apply(JobStorageTransaction transaction)
        {
            if (transaction == null)
                throw new ArgumentNullException(nameof(transaction));
            
            transaction.PersistSet(ConsoleId.GetSetKey());
            transaction.PersistHash(ConsoleId.GetHashKey());

            // After upgrading to Hangfire.Console version with new keys, 
            // there may be existing background jobs with console attached 
            // to the previous keys. We should persist them also.
            transaction.PersistSet(ConsoleId.GetOldConsoleKey());
            transaction.PersistHash(ConsoleId.GetOldConsoleKey());
        }
    }
}