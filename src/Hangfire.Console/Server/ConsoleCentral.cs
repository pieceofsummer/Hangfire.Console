using System;
using System.Collections.Concurrent;
using Hangfire.Console.Serialization;
using Hangfire.Console.Storage;
using Hangfire.Console.Storage.Operations;
using Hangfire.Console.Utils;
using Hangfire.Logging;
using Hangfire.Storage;

namespace Hangfire.Console.Server
{
    internal partial class ConsoleCentral
    {
        private const int OperationsQueueBoundedCapacity = 1000;
        
        private static readonly ILog Log = LogProvider.For<ConsoleCentral>();
        
        public ConsoleCentral(string serverId, JobStorage storage)
        {
            ServerId = serverId ?? throw new ArgumentNullException(nameof(serverId));
            Storage = storage ?? throw new ArgumentNullException(nameof(storage));

            _operationsQueueInner = new AggregateQueue<Operation, OperationKey>(
                x => x.CreateKey(), Aggregate, PlaceholderOperation.Instance);
            
            OperationsQueue = new BlockingCollection<Operation>(
                _operationsQueueInner, OperationsQueueBoundedCapacity);
        }

        public string ServerId { get; }
        
        public JobStorage Storage { get; }
        
        public BlockingCollection<Operation> OperationsQueue { get; }
        
        private readonly AggregateQueue<Operation, OperationKey> _operationsQueueInner;
        
        private static bool Aggregate(OperationKey key, Operation item, Operation update)
        {
            return Aggregate((ProgressBarOperation) item, (ProgressBarOperation) update);
        }

        private static bool Aggregate(ProgressBarOperation item, ProgressBarOperation update)
        {
            item.Value = update.Value;
            item.TimeOffset = update.TimeOffset;
            return true;
        }
        
        private sealed class PlaceholderOperation : Operation
        {
            public static readonly Operation Instance = new PlaceholderOperation(new ConsoleId("1", 1));

            private PlaceholderOperation(ConsoleId consoleId) : base(consoleId) { }

            public override void Apply(JobStorageTransaction transaction) { }
        }
    }
}