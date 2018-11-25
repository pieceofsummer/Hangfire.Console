using System;
using System.Collections.Concurrent;
using Hangfire.Logging;
using Hangfire.Storage;

namespace Hangfire.Console.Runtime
{
    /// <summary>
    /// Provides run-time information about JobStorage connection/transaction classes.
    /// </summary>
    internal static class JobStorageInfo
    {
        private static readonly ConcurrentDictionary<Type, IJobStorageInfo> Cache = new ConcurrentDictionary<Type, IJobStorageInfo>();
        private static readonly ILog Log = LogProvider.For<IJobStorageInfo>();
        
        private sealed class Details : IJobStorageInfo
        {
            public Details(IStorageConnection connection, IWriteOnlyTransaction transaction)
            {
                ConnectionType = connection.GetType();
                SupportsJobStorageConnection = connection is JobStorageConnection;
                TransactionType = transaction.GetType();
                SupportsJobStorageTransaction = transaction is JobStorageTransaction;
                
                // Job Storage should implement full API:
                IsSupported = SupportsJobStorageConnection &&
                              SupportsJobStorageTransaction;
            }
            
            public Type ConnectionType { get; }
            public bool SupportsJobStorageConnection { get; }
            public Type TransactionType { get; }
            public bool SupportsJobStorageTransaction { get; }
            public bool IsSupported { get; }
        }
        
        private static void WriteDetailsToLog(IJobStorageInfo info)
        {
            if (!info.SupportsJobStorageConnection)
                Log.WarnFormat("Connection class {0} is not a subclass of JobStorageConnection", info.ConnectionType);
                
            if (!info.SupportsJobStorageTransaction)
                Log.WarnFormat("Transaction class {0} is not a subclass of JobStorageTransaction", info.TransactionType);

            if (!info.IsSupported)
                Log.Warn("Console can't work with your JobStorage provider");
        }
        
        #if DEBUG
        
        internal static void MockSetup(IStorageConnection connection, IWriteOnlyTransaction transaction)
        {
            Cache[connection.GetType()] = new Details(connection, transaction);
        }
    
        #endif

        public static void Reset() => Cache.Clear();
        
        public static IJobStorageInfo Get(JobStorage storage)
        {
            if (storage == null)
                throw new ArgumentNullException(nameof(storage));

            return Cache.GetOrAdd(storage.GetType(), _ =>
            {
                using (var connection = storage.GetConnection())
                    return Get(connection);
            });
        }
        
        public static IJobStorageInfo Get(IStorageConnection connection)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));
            
            return Cache.GetOrAdd(connection.GetType(), _ =>
            {
                using (var transaction = connection.CreateWriteTransaction())
                {
                    var info = new Details(connection, transaction);
                    WriteDetailsToLog(info);
                    return info;
                }
            });
        }
        
        public static bool IsSupported(this JobStorage storage) => Get(storage).IsSupported;
        
        public static bool IsSupported(this IStorageConnection connection) => Get(connection).IsSupported;
    }
}