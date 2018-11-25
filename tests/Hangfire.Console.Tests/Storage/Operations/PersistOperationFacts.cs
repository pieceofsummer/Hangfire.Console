using System;
using Hangfire.Console.Serialization;
using Hangfire.Console.Storage;
using Hangfire.Console.Storage.Operations;
using Hangfire.Storage;
using Moq;
using Xunit;

namespace Hangfire.Console.Tests.Storage.Operations
{
    public class PersistOperationFacts
    {
        private readonly ConsoleId _consoleId;
        private readonly Mock<JobStorageTransaction> _transaction;

        public PersistOperationFacts()
        {
            _consoleId = new ConsoleId("1", DateTime.UtcNow);
            _transaction = new Mock<JobStorageTransaction>();
        }

        [Fact]
        public void Ctor_ThrowsException_IfConsoleIdIsNull()
        {
            Assert.Throws<ArgumentNullException>("consoleId", () => CreateOperation(null));
        }

        [Fact]
        public void Apply_ThrowsException_IfTransactionIsNull()
        {
            var operation = CreateOperation(_consoleId);

            Assert.Throws<ArgumentNullException>("transaction", () => operation.Apply(null));
        }
        
        private PersistOperation CreateOperation(ConsoleId consoleId) => new PersistOperation(consoleId);

        [Fact]
        public void Execute()
        {
            var operation = CreateOperation(_consoleId);

            operation.Apply(_transaction.Object);

            _transaction.Verify(x => x.PersistSet(_consoleId.GetSetKey()));
            _transaction.Verify(x => x.PersistHash(_consoleId.GetHashKey()));

            // backward compatibility:
            _transaction.Verify(x => x.PersistSet(_consoleId.GetOldConsoleKey()));
            _transaction.Verify(x => x.PersistHash(_consoleId.GetOldConsoleKey()));
        }
    }
}