using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Hangfire.Console.Storage;
using Hangfire.Logging;
using Hangfire.Storage;

namespace Hangfire.Console.Server
{
    internal partial class ConsoleCentral : IConsoleCentralWorker
    {
        private const int MaxOperationsPerTransaction = 20;
        
        public void Execute(CancellationToken cancellationToken)
        {
            var operations = new List<Operation>(MaxOperationsPerTransaction);
            var stopwatch = new Stopwatch();
            var lastStatistics = DateTime.MinValue;
            
            using (var connection = Storage.GetConnection())
            {
                Log.DebugFormat("Worker#{0}: started", Thread.CurrentThread.ManagedThreadId);
                
                foreach (var operation in OperationsQueue.GetConsumingEnumerable(cancellationToken))
                {
                    if (operation == PlaceholderOperation.Instance)
                    {
                        // ignore placeholders
                        continue;
                    }
                    
                    operations.Add(operation);

                    if (operations.Count < MaxOperationsPerTransaction && OperationsQueue.Count > 0)
                    {
                        // need more for a batch
                        continue;
                    }
                    
                    stopwatch.Restart();
                    
                    using (var transaction = (JobStorageTransaction) connection.CreateWriteTransaction())
                    {
                        // ReSharper disable once AccessToDisposedClosure
                        operations.ForEach(x => x.Apply(transaction));

                        transaction.Commit();
                    }
                    
                    stopwatch.Stop();
                    
                    Log.DebugFormat("[{0:yyyy-MM-dd HH:mm:ss.fff}] Committed {1} operations in {2} ms", 
                                    DateTime.Now, operations.Count, stopwatch.ElapsedMilliseconds);
                    
                    operations.Clear();

                    if (lastStatistics < DateTime.UtcNow)
                    {
                        Log.DebugFormat("[QUEUED OPERATIONS STATS] IN = {0}, ENQ = {1}, DEQ = {2}, LEN = {3}, OPT = {4}",
                                        _operationsQueueInner.CountInput, _operationsQueueInner.CountEnqueued,
                                        _operationsQueueInner.CountDequeued, _operationsQueueInner.CountInQueue,
                                        _operationsQueueInner.CountInput - _operationsQueueInner.CountEnqueued);
                        
                        lastStatistics = DateTime.UtcNow.AddSeconds(10);
                    }
                }
                
                Log.DebugFormat("Worker#{0}: finished", Thread.CurrentThread.ManagedThreadId);
            }
        }

        public void Dispose()
        {
            Log.DebugFormat("Worker#{0}: shutting down", Thread.CurrentThread.ManagedThreadId);
            
            // TODO: cleanup on server shutdown
        }
    }
}