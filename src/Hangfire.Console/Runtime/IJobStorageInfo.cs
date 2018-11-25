using System;
using Hangfire.Storage;

namespace Hangfire.Console.Runtime
{
    /// <summary>
    /// Provides run-time information about JobStorage connection/transaction classes.
    /// </summary>
    internal interface IJobStorageInfo
    {
        /// <summary>
        /// Job storage connection type
        /// </summary>
        Type ConnectionType { get; }
        
        /// <summary>
        /// True if <see cref="ConnectionType"/> is a subclass of <see cref="JobStorageConnection"/>.
        /// </summary>
        bool SupportsJobStorageConnection { get; }
        
        /// <summary>
        /// Job storage transaction type
        /// </summary>
        Type TransactionType { get; }
        
        /// <summary>
        /// True if <see cref="TransactionType"/> is a subclass of <see cref="JobStorageTransaction"/>.
        /// </summary>
        bool SupportsJobStorageTransaction { get; }
        
        /// <summary>
        /// Returns if the project is compatible with this storage.
        /// </summary>
        bool IsSupported { get; }
    }
}