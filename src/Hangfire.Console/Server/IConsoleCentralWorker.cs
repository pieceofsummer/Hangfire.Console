using System;
using System.Threading;

namespace Hangfire.Console.Server
{
    internal interface IConsoleCentralWorker : IDisposable
    {
        string ServerId { get; }
        
        void Execute(CancellationToken cancellationToken);
    }
}