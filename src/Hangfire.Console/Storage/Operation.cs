using System;
using Hangfire.Console.Serialization;
using Hangfire.Storage;

namespace Hangfire.Console.Storage
{
    /// <summary>
    /// Base class for all operations
    /// </summary>
    internal abstract class Operation
    {
        /// <summary>
        /// Initializes a new instance for <paramref name="consoleId"/>.
        /// </summary>
        protected Operation(ConsoleId consoleId)
        {
            ConsoleId = consoleId ?? throw new ArgumentNullException(nameof(consoleId));
        }
        
        /// <summary>
        /// Gets the target console id.
        /// </summary>
        public ConsoleId ConsoleId { get; }

        /// <summary>
        /// Returns the number of child operations (for container operations).
        /// </summary>
        public virtual int Count => 1;
        
        /// <summary>
        /// Performs operation-specific actions on the <paramref name="transaction"/>.
        /// </summary>
        /// <param name="transaction">Transaction</param>
        public abstract void Apply(JobStorageTransaction transaction);

        /// <summary>
        /// Generates an optional <see cref="OperationKey"/> for the operation.
        /// </summary>
        /// <returns>Operation key instance, or <c>null</c> if no matching needed</returns>
        public virtual OperationKey CreateKey() => null;
    }
}