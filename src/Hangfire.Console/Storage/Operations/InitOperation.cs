using System;
using System.Collections.Generic;
using Hangfire.Console.Serialization;
using Hangfire.Storage;

namespace Hangfire.Console.Storage.Operations
{
    /// <summary>
    /// Init operation
    /// </summary>
    internal class InitOperation : Operation
    {
        /// <inheritdoc />
        public InitOperation(ConsoleId consoleId) : base(consoleId)
        {
        }

        /// <inheritdoc />
        public override void Apply(JobStorageTransaction transaction)
        {
            if (transaction == null)
                throw new ArgumentNullException(nameof(transaction));
            
            transaction.SetRangeInHash(ConsoleId.GetHashKey(), new[] { new KeyValuePair<string, string>("jobId", ConsoleId.JobId) });
        }
    }
}