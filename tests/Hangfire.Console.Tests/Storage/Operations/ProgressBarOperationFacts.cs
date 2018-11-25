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
    public class ProgressBarOperationFacts
    {
        private readonly ConsoleId _consoleId;
        private readonly Mock<JobStorageTransaction> _transaction;

        public ProgressBarOperationFacts()
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
        
        private ProgressBarOperation CreateOperation(ConsoleId consoleId) => new ProgressBarOperation(consoleId, new ConsoleLine() {ProgressValue = 0});

        [Fact]
        public void Ctor_ThrowsException_IfLineIsNull()
        {
            Assert.Throws<ArgumentNullException>("line", () => new ProgressBarOperation(_consoleId, null));
        }

        [Fact]
        public void Ctor_ThrowsException_IfLineIsReference()
        {
            Assert.Throws<ArgumentException>("line", () => new ProgressBarOperation(_consoleId, new ConsoleLine() {IsReference = true}));
        }

        [Fact]
        public void Ctor_ThrowsException_IfLineIsNotProgressBar()
        {
            Assert.Throws<ArgumentException>("line", () => new ProgressBarOperation(_consoleId, new ConsoleLine()));
        }

        [Fact]
        public void Execute()
        {
            var line = new ConsoleLine()
            {
                Message = "1",
                ProgressValue = 10
            };

            var operation = new ProgressBarOperation(_consoleId, line);

            operation.Apply(_transaction.Object);

            _transaction.Verify(x => x.AddToSet(_consoleId.GetSetKey(), It.IsAny<string>(), It.IsAny<double>()), Times.Once);
            _transaction.Verify(x => x.SetRangeInHash(_consoleId.GetHashKey(), It2.AnyIs<KVP>(p => p.Key == "progress")), Times.Once);
            _transaction.Verify(x => x.SetRangeInHash(_consoleId.GetHashKey(), It2.AnyIs<KVP>(p => p.Key != "progress")), Times.Never);
        }
    }
}