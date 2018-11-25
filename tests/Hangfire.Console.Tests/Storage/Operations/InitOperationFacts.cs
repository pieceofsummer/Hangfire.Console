using System;
using Hangfire.Console.Serialization;
using Hangfire.Console.Storage;
using Hangfire.Console.Storage.Operations;
using Hangfire.Storage;
using Moq;
using Xunit;
using KVP = System.Collections.Generic.KeyValuePair<string, string>;

namespace Hangfire.Console.Tests.Storage.Operations
{
    public class InitOperationFacts
    {
        private readonly ConsoleId _consoleId;
        private readonly Mock<JobStorageTransaction> _transaction;

        public InitOperationFacts()
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
        
        private InitOperation CreateOperation(ConsoleId consoleId) => new InitOperation(consoleId);

        [Fact]
        public void Execute()
        {
            var operation = CreateOperation(_consoleId);

            operation.Apply(_transaction.Object);

            _transaction.Verify(x => x.SetRangeInHash(_consoleId.GetHashKey(), It2.AnyIs<KVP>(p => p.Key == "jobId" && p.Value == _consoleId.JobId)));
        }
    }
}