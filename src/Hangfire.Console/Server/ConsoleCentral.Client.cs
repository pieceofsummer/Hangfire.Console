using System;
using Hangfire.Console.Serialization;
using Hangfire.Console.Storage;

namespace Hangfire.Console.Server
{
    internal partial class ConsoleCentral : IConsoleCentralClient
    {
        public IOperationStream CreateConsoleStream(ConsoleId consoleId) => new ConsoleStream(consoleId, this);
        
        public void Write(Operation operation)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));
            
            OperationsQueue.Add(operation);
        }
        
        private class ConsoleStream : IOperationStream
        {
            private readonly ConsoleId _consoleId;
            private readonly ConsoleCentral _central;

            public ConsoleStream(ConsoleId consoleId, ConsoleCentral central)
            {
                _consoleId = consoleId ?? throw new ArgumentNullException(nameof(consoleId));
                _central = central ?? throw new ArgumentNullException(nameof(central));
            }

            public void Write(Operation operation)
            {
                if (operation == null)
                    throw new ArgumentNullException(nameof(operation));
                if (operation.ConsoleId != _consoleId)
                    throw new ArgumentException("ConsoleId mismatch", nameof(operation));
                    
                _central.Write(operation);
            }

            public void Flush()
            {
            }
        }
    }
}