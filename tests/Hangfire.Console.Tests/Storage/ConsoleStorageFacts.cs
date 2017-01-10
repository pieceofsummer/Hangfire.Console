using Hangfire.Common;
using Hangfire.Console.Serialization;
using Hangfire.Console.Storage;
using Hangfire.States;
using Hangfire.Storage;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Hangfire.Console.Tests.Storage
{
    public class ConsoleStorageFacts
    {
        private readonly ConsoleId _consoleId;

        private readonly Mock<JobStorageConnection> _connection;
        private readonly Mock<JobStorageTransaction> _transaction;

        public ConsoleStorageFacts()
        {
            _consoleId = new ConsoleId("1", DateTime.UtcNow);

            _connection = new Mock<JobStorageConnection>();
            _transaction = new Mock<JobStorageTransaction>();

            _connection.Setup(x => x.CreateWriteTransaction())
                .Returns(_transaction.Object);
        }

        [Fact]
        public void Ctor_ThrowsException_IfConnectionIsNull()
        {
            Assert.Throws<ArgumentNullException>("connection", () => new ConsoleStorage(null));
        }

        [Fact]
        public void Dispose_ReallyDisposesConnection()
        {
            var storage = new ConsoleStorage(_connection.Object);

            storage.Dispose();

            _connection.Verify(x => x.Dispose());
        }

        [Fact]
        public void AddLine_ThrowsException_IfConsoleIdIsNull()
        {
            var storage = new ConsoleStorage(_connection.Object);

            Assert.Throws<ArgumentNullException>("consoleId", () => storage.AddLine(null, new ConsoleLine()));
        }

        [Fact]
        public void AddLine_ThrowsException_IfLineIsNull()
        {
            var storage = new ConsoleStorage(_connection.Object);

            Assert.Throws<ArgumentNullException>("line", () => storage.AddLine(_consoleId, null));
        }

        [Fact]
        public void AddLine_ThrowsException_IfLineIsReference()
        {
            var storage = new ConsoleStorage(_connection.Object);

            Assert.Throws<ArgumentException>("line", () => storage.AddLine(_consoleId, new ConsoleLine() { IsReference = true }));
        }

        [Fact]
        public void AddLine_ShortLineIsAddedToSet()
        {
            var storage = new ConsoleStorage(_connection.Object);
            var line = new ConsoleLine() { Message = "test" };

            storage.AddLine(_consoleId, line);

            Assert.False(line.IsReference);
            _transaction.Verify(x => x.AddToSet($"console:{_consoleId}", It.IsAny<string>(), It.IsAny<double>()));
            _transaction.Verify(x => x.SetRangeInHash($"console:refs:{_consoleId}", It.IsAny<IEnumerable<KeyValuePair<string, string>>>()), Times.Never);
        }

        [Fact]
        public void AddLine_LongLineIsAddedToHash_AndReferenceIsAddedToSet()
        {
            var storage = new ConsoleStorage(_connection.Object);
            var line = new ConsoleLine()
            {
                Message = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor " +
                          "incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud " +
                          "exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure " +
                          "dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. " +
                          "Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum."
            };

            storage.AddLine(_consoleId, line);

            Assert.True(line.IsReference);
            _transaction.Verify(x => x.AddToSet($"console:{_consoleId}", It.IsAny<string>(), It.IsAny<double>()));
            _transaction.Verify(x => x.SetRangeInHash($"console:refs:{_consoleId}", It.IsAny<IEnumerable<KeyValuePair<string, string>>>()));
        }

        [Fact]
        public void Expire_ThrowsException_IfConsoleIdIsNull()
        {
            var storage = new ConsoleStorage(_connection.Object);

            Assert.Throws<ArgumentNullException>("consoleId", () => storage.Expire(null, TimeSpan.FromHours(1)));
        }

        [Fact]
        public void Expire_ExpiresSetAndHash()
        {
            var storage = new ConsoleStorage(_connection.Object);

            storage.Expire(_consoleId, TimeSpan.FromHours(1));

            _transaction.Verify(x => x.ExpireSet($"console:{_consoleId}", It.IsAny<TimeSpan>()));
            _transaction.Verify(x => x.ExpireHash($"console:refs:{_consoleId}", It.IsAny<TimeSpan>()));
        }

        [Fact]
        public void Expire_ExpiresOldSetAndHashKeysEither_ForBackwardsCompatibility()
        {
            var storage = new ConsoleStorage(_connection.Object);

            storage.Expire(_consoleId, TimeSpan.FromHours(1));

            _transaction.Verify(x => x.ExpireSet(_consoleId.ToString(), It.IsAny<TimeSpan>()));
            _transaction.Verify(x => x.ExpireHash(_consoleId.ToString(), It.IsAny<TimeSpan>()));
        }

        [Fact]
        public void GetLineCount_ThrowsException_IfConsoleIdIsNull()
        {
            var storage = new ConsoleStorage(_connection.Object);

            Assert.Throws<ArgumentNullException>("consoleId", () => storage.GetLineCount(null));
        }

        [Fact]
        public void GetLineCount_ReturnsCountOfSet()
        {
            _connection.Setup(x => x.GetSetCount($"console:{_consoleId}"))
                .Returns(123);

            var storage = new ConsoleStorage(_connection.Object);

            var count = storage.GetLineCount(_consoleId);

            Assert.Equal(123, count);
        }

        [Fact]
        public void GetLineCount_ReturnsCountOfOldSet_WhenNewOneReturnsZero_ForBackwardsCompatibility()
        {
            _connection.Setup(x => x.GetSetCount($"console:{_consoleId}")).Returns(0);
            _connection.Setup(x => x.GetSetCount(_consoleId.ToString())).Returns(123);

            var storage = new ConsoleStorage(_connection.Object);

            var count = storage.GetLineCount(_consoleId);

            Assert.Equal(123, count);
        }

        [Fact]
        public void GetLines_ThrowsException_IfConsoleIdIsNull()
        {
            var storage = new ConsoleStorage(_connection.Object);

            Assert.Throws<ArgumentNullException>("consoleId", () => storage.GetLines(null, 0, 1).ToArray());
        }

        [Fact]
        public void GetLines_ReturnsRangeFromSet()
        {
            var lines = new[] {
                new ConsoleLine { TimeOffset = 0, Message = "line1" },
                new ConsoleLine { TimeOffset = 1, Message = "line2" },
                new ConsoleLine { TimeOffset = 2, Message = "line3" },
                new ConsoleLine { TimeOffset = 3, Message = "line4" },
            };

            _connection.Setup(x => x.GetRangeFromSet($"console:{_consoleId}", It.IsAny<int>(), It.IsAny<int>()))
                .Returns((string key, int start, int end) => lines.Where((x, i) => i >= start && i <= end).Select(JobHelper.ToJson).ToList());

            var storage = new ConsoleStorage(_connection.Object);

            var result = storage.GetLines(_consoleId, 1, 2).ToArray();

            Assert.Equal(lines.Skip(1).Take(2).Select(x => x.Message), result.Select(x => x.Message));
        }

        [Fact]
        public void GetLines_ReturnsRangeFromOldSet_ForBackwardsCompatibility()
        {
            var lines = new[] {
                new ConsoleLine { TimeOffset = 0, Message = "line1" },
                new ConsoleLine { TimeOffset = 1, Message = "line2" },
                new ConsoleLine { TimeOffset = 2, Message = "line3" },
                new ConsoleLine { TimeOffset = 3, Message = "line4" },
            };

            _connection.Setup(x => x.GetRangeFromSet(_consoleId.ToString(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns((string key, int start, int end) => lines.Where((x, i) => i >= start && i <= end).Select(JobHelper.ToJson).ToList());

            var storage = new ConsoleStorage(_connection.Object);

            var result = storage.GetLines(_consoleId, 1, 2).ToArray();

            Assert.Equal(lines.Skip(1).Take(2).Select(x => x.Message), result.Select(x => x.Message));
        }

        [Fact]
        public void GetLines_ExpandsReferences()
        {
            var lines = new[] {
                new ConsoleLine { TimeOffset = 0, Message = "line1", IsReference = true }
            };

            _connection.Setup(x => x.GetRangeFromSet($"console:{_consoleId}", It.IsAny<int>(), It.IsAny<int>()))
                .Returns((string key, int start, int end) => lines.Where((x, i) => i >= start && i <= end).Select(JobHelper.ToJson).ToList());
            _connection.Setup(x => x.GetValueFromHash($"console:refs:{_consoleId}", It.IsAny<string>()))
                .Returns("Dereferenced Line");

            var storage = new ConsoleStorage(_connection.Object);

            var result = storage.GetLines(_consoleId, 0, 1).Single();

            Assert.False(result.IsReference);
            Assert.Equal("Dereferenced Line", result.Message);
        }

        [Fact]
        public void GetLines_ExpandsReferencesFromOldHash_ForBackwardsCompatibility()
        {
            var lines = new[] {
                new ConsoleLine { TimeOffset = 0, Message = "line1", IsReference = true }
            };

            _connection.Setup(x => x.GetRangeFromSet(_consoleId.ToString(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns((string key, int start, int end) => lines.Where((x, i) => i >= start && i <= end).Select(JobHelper.ToJson).ToList());
            _connection.Setup(x => x.GetValueFromHash(_consoleId.ToString(), It.IsAny<string>()))
                .Returns("Dereferenced Line");

            var storage = new ConsoleStorage(_connection.Object);

            var result = storage.GetLines(_consoleId, 0, 1).Single();

            Assert.False(result.IsReference);
            Assert.Equal("Dereferenced Line", result.Message);
        }

        [Fact]
        public void GetLines_HandlesHashException_WhenTryingToExpandReferences()
        {
            var lines = new[] {
                new ConsoleLine { TimeOffset = 0, Message = "line1", IsReference = true }
            };

            _connection.Setup(x => x.GetRangeFromSet(_consoleId.ToString(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns((string key, int start, int end) => lines.Where((x, i) => i >= start && i <= end).Select(JobHelper.ToJson).ToList());

            _connection.Setup(x => x.GetValueFromHash(_consoleId.ToString(), It.IsAny<string>()))
                .Throws(new NotSupportedException());

            var storage = new ConsoleStorage(_connection.Object);

            var result = storage.GetLines(_consoleId, 0, 1).Single();

            Assert.False(result.IsReference);
            Assert.Equal("line1", result.Message);
        }

        [Fact]
        public void GetState_ThrowsException_IfConsoleIdIsNull()
        {
            var storage = new ConsoleStorage(_connection.Object);

            Assert.Throws<ArgumentNullException>("consoleId", () => storage.GetState(null));
        }

        [Fact]
        public void GetState_ReturnsStateData()
        {
            var state = new StateData()
            {
                Name = ProcessingState.StateName,
                Data = new Dictionary<string, string>()
            };

            _connection.Setup(x => x.GetStateData(It.IsAny<string>()))
                .Returns(state);

            var storage = new ConsoleStorage(_connection.Object);

            var result = storage.GetState(_consoleId);

            Assert.Same(state, result);
        }
    }
}
