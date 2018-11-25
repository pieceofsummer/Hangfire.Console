using System;
using System.Collections.Generic;
using Hangfire.Console.Serialization;
using Hangfire.Console.Storage;
using Hangfire.Console.Storage.Operations;
using Hangfire.Storage;
using Moq;
using Xunit;
using KVP = System.Collections.Generic.KeyValuePair<string, string>;

namespace Hangfire.Console.Tests.Storage.Operations
{
    public class LineOperationFacts
    {
        private readonly ConsoleId _consoleId;
        private readonly Mock<JobStorageTransaction> _transaction;

        public LineOperationFacts()
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
        
        private LineOperation CreateOperation(ConsoleId consoleId) => new LineOperation(consoleId, new ConsoleLine());

        [Fact]
        public void Ctor_ThrowsException_IfLineIsNull()
        {
            Assert.Throws<ArgumentNullException>("line", () => new LineOperation(_consoleId, null));
        }

        [Fact]
        public void Ctor_ThrowsException_IfLineIsReference()
        {
            Assert.Throws<ArgumentException>("line", () => new LineOperation(_consoleId, new ConsoleLine() {IsReference = true}));
        }

        [Fact]
        public void Execute_ShortLine()
        {
            var line = new ConsoleLine() {Message = "test"};

            var operation = new LineOperation(_consoleId, line);

            operation.Apply(_transaction.Object);

            _transaction.Verify(x => x.AddToSet(_consoleId.GetSetKey(), It.IsAny<string>(), It.IsAny<double>()), Times.Once);
            _transaction.Verify(x => x.SetRangeInHash(_consoleId.GetHashKey(), It.IsAny<IEnumerable<KVP>>()), Times.Never);
        }

        [Fact]
        public void Execute_LongLine()
        {
            var line = new ConsoleLine()
            {
                Message = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor " +
                          "incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud " +
                          "exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure " +
                          "dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. " +
                          "Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum."
            };

            var operation = new LineOperation(_consoleId, line);

            operation.Apply(_transaction.Object);

            _transaction.Verify(x => x.AddToSet(_consoleId.GetSetKey(), It.IsAny<string>(), It.IsAny<double>()), Times.Once);
            _transaction.Verify(x => x.SetRangeInHash(_consoleId.GetHashKey(), It2.AnyIs<KVP>(p => p.Value == line.Message)), Times.Once);
            _transaction.Verify(x => x.SetRangeInHash(_consoleId.GetHashKey(), It2.AnyIs<KVP>(p => p.Key == "progress")), Times.Never);
        }
    }
}