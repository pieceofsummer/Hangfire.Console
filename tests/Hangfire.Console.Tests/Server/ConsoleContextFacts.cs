using Hangfire.Console.Serialization;
using Hangfire.Console.Server;
using Hangfire.Console.Storage;
using Moq;
using System;
using Xunit;

namespace Hangfire.Console.Tests.Server
{
    public class ConsoleContextFacts
    {
        private readonly Mock<IConsoleStorage> _storage;

        public ConsoleContextFacts()
        {
            _storage = new Mock<IConsoleStorage>();
        }

        [Fact]
        public void Ctor_ThrowsException_IfConsoleIdIsNull()
        {
            Assert.Throws<ArgumentNullException>("consoleId", () => new ConsoleContext(null, _storage.Object));
        }

        [Fact]
        public void Ctor_ThrowsException_IfStorageIsNull()
        {
            var consoleId = new ConsoleId("1", new DateTime(2016, 1, 1, 0, 0, 0, DateTimeKind.Utc));

            Assert.Throws<ArgumentNullException>("storage", () => new ConsoleContext(consoleId, null));
        }

        [Fact]
        public void AddLine_ThrowsException_IfLineIsNull()
        {
            var consoleId = new ConsoleId("1", new DateTime(2016, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            var context = new ConsoleContext(consoleId, _storage.Object);

            Assert.Throws<ArgumentNullException>("line", () => context.AddLine(null));
        }

        [Fact]
        public void AddLine_ReallyAddsLine()
        {
            var consoleId = new ConsoleId("1", new DateTime(2016, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            var context = new ConsoleContext(consoleId, _storage.Object);

            context.AddLine(new ConsoleLine() { TimeOffset = 0, Message = "line" });

            _storage.Verify(x => x.AddLine(It.IsAny<ConsoleId>(), It.IsAny<ConsoleLine>()));
        }

        [Fact]
        public void AddLine_CorrectsTimeOffset()
        {
            var consoleId = new ConsoleId("1", new DateTime(2016, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            var context = new ConsoleContext(consoleId, _storage.Object);

            var line1 = new ConsoleLine() { TimeOffset = 0, Message = "line" };
            var line2 = new ConsoleLine() { TimeOffset = 0, Message = "line" };

            context.AddLine(line1);
            context.AddLine(line2);

            _storage.Verify(x => x.AddLine(It.IsAny<ConsoleId>(), It.IsAny<ConsoleLine>()), Times.Exactly(2));
            Assert.NotEqual(line1.TimeOffset, line2.TimeOffset, 4);
        }

        [Fact]
        public void WriteLine_ReallyAddsLine()
        {
            var consoleId = new ConsoleId("1", new DateTime(2016, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            var context = new ConsoleContext(consoleId, _storage.Object);

            context.AddLine(new ConsoleLine() { TimeOffset = 0, Message = "line" });

            _storage.Verify(x => x.AddLine(It.IsAny<ConsoleId>(), It.IsAny<ConsoleLine>()));
        }

        [Fact]
        public void WriteProgressBar_WritesDefaultValue_AndReturnsNonNull()
        {
            var consoleId = new ConsoleId("1", new DateTime(2016, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            var context = new ConsoleContext(consoleId, _storage.Object);

            var progressBar = context.WriteProgressBar(0, null);

            _storage.Verify(x => x.AddLine(It.IsAny<ConsoleId>(), It.IsAny<ConsoleLine>()));
            Assert.NotNull(progressBar);
        }
        
        [Fact]
        public void Expire_ReallyExpiresLines()
        {
            var consoleId = new ConsoleId("1", new DateTime(2016, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            var context = new ConsoleContext(consoleId, _storage.Object);

            context.Expire(TimeSpan.FromHours(1));

            _storage.Verify(x => x.Expire(It.IsAny<ConsoleId>(), It.IsAny<TimeSpan>()));
        }

    }
}
