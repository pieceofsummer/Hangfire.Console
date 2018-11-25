using System;
using System.Collections.Concurrent;
using System.Threading;
using Hangfire.Server;

namespace Hangfire.Console.Server
{
    internal partial class ConsoleCentral
    {
        private static readonly ConcurrentDictionary<string, ConsoleCentral> Instances 
            = new ConcurrentDictionary<string, ConsoleCentral>(StringComparer.OrdinalIgnoreCase);

        private static readonly ConcurrentDictionary<JobStorage, ConsoleCentral> ByStorage
            = new ConcurrentDictionary<JobStorage, ConsoleCentral>();
        
        internal static bool TryGetCentral(string serverId, out IConsoleCentralClient central)
        {
            central = null;
            if (string.IsNullOrEmpty(serverId)) return false;
            
            if (!Instances.TryGetValue(serverId, out var instance)) return false;

            central = instance;
            return true;
        }
        
        internal static bool TryGetCentral(JobStorage storage, out IConsoleCentralClient central)
        {
            central = null;
            if (storage == null) return false;
            
            if (!ByStorage.TryGetValue(storage, out var instance)) return false;
            
            central = instance;
            return true;
        }

        internal static IConsoleCentralWorker GetWorker(BackgroundProcessContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            
            if (!context.IsShutdownRequested)
            {
                ConsoleCentral ConsoleCentralFactory(string serverId)
                {
                    // TODO: perform one-time compatibility checks

                    return new ConsoleCentral(serverId, context.Storage);
                }

                var central = Instances.GetOrAdd(context.ServerId, ConsoleCentralFactory);

                ByStorage.TryAdd(context.Storage, central);
                
                return new ConsoleCentralWorkerWrapper(central, context.CancellationToken);
            }
            else if (Instances.TryRemove(context.ServerId, out var central))
            {
                ByStorage.TryRemove(context.Storage, out _);
                
                return new ConsoleCentralWorkerWrapper(central, context.CancellationToken);
            }

            return default(ConsoleCentralWorkerWrapper);
        }
        
        private struct ConsoleCentralWorkerWrapper : IConsoleCentralWorker
        {
            public ConsoleCentralWorkerWrapper(IConsoleCentralWorker worker, CancellationToken shutdownToken)
            {
                Worker = worker ?? throw new ArgumentNullException(nameof(worker));
                ShutdownToken = shutdownToken;
            }

            private IConsoleCentralWorker Worker { get; }

            private CancellationToken ShutdownToken { get; }

            /// <inheritdoc />
            public string ServerId => Worker?.ServerId ?? "";
            
            /// <inheritdoc />
            public void Execute(CancellationToken cancellationToken) => Worker?.Execute(cancellationToken);

            /// <inheritdoc />
            public void Dispose()
            {
                if (ShutdownToken.IsCancellationRequested && Instances.TryRemove(ServerId, out var central))
                {
                    ByStorage.TryRemove(central.Storage, out _);
                    central.Dispose();
                }
            }
        }
    }
}