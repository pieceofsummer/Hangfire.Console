using System;
using Hangfire.Console.Serialization;
using Hangfire.Console.Storage;
using Hangfire.Console.Storage.Operations;
using Hangfire.Storage;
using Moq;
using Xunit;

namespace Hangfire.Console.Tests.Storage.Operations
{
    public class ExpireOperationFacts
    {
        private readonly ConsoleId _consoleId;
        private readonly Mock<JobStorageTransaction> _transaction;

        public ExpireOperationFacts()
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
        
        private ExpireOperation CreateOperation(ConsoleId consoleId) => new ExpireOperation(consoleId, TimeSpan.FromHours(1));

        [Fact]
        public void Execute()
        {
            var operation = CreateOperation(_consoleId);

            operation.Apply(_transaction.Object);

            _transaction.Verify(x => x.ExpireSet(_consoleId.GetSetKey(), It.IsAny<TimeSpan>()));
            _transaction.Verify(x => x.ExpireHash(_consoleId.GetHashKey(), It.IsAny<TimeSpan>()));

            // backward compatibility:
            _transaction.Verify(x => x.ExpireSet(_consoleId.GetOldConsoleKey(), It.IsAny<TimeSpan>()));
            _transaction.Verify(x => x.ExpireHash(_consoleId.GetOldConsoleKey(), It.IsAny<TimeSpan>()));
        }
    }
}