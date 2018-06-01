using System;
using Hangfire.Console.Serialization;
using Hangfire.Storage;

namespace Hangfire.Console.Storage
{
    internal class ConsoleExpirationTransaction : IConsoleExpirationTransaction
    {
        private readonly JobStorageTransaction _transaction;

        public ConsoleExpirationTransaction(JobStorageTransaction transaction)
        {
            _transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
        }

        public void Dispose()
        {
            _transaction.Dispose();
        }
        
        public void Expire(ConsoleId consoleId, TimeSpan expireIn)
        {
            if (consoleId == null)
                throw new ArgumentNullException(nameof(consoleId));

            _transaction.ExpireSet(consoleId.GetSetKey(), expireIn);
            _transaction.ExpireHash(consoleId.GetHashKey(), expireIn);

            // After upgrading to Hangfire.Console version with new keys, 
            // there may be existing background jobs with console attached 
            // to the previous keys. We should expire them also.
            _transaction.ExpireSet(consoleId.GetOldConsoleKey(), expireIn);
            _transaction.ExpireHash(consoleId.GetOldConsoleKey(), expireIn);
        }

        public void Persist(ConsoleId consoleId)
        {
            if (consoleId == null)
                throw new ArgumentNullException(nameof(consoleId));

            _transaction.PersistSet(consoleId.GetSetKey());
            _transaction.PersistHash(consoleId.GetHashKey());

            // After upgrading to Hangfire.Console version with new keys, 
            // there may be existing background jobs with console attached 
            // to the previous keys. We should persist them also.
            _transaction.PersistSet(consoleId.GetOldConsoleKey());
            _transaction.PersistHash(consoleId.GetOldConsoleKey());
        }
    }
}
