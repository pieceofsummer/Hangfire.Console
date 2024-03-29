﻿using Hangfire.Console.Serialization;
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

        private readonly ConsoleOptions _options;

        public ConsoleContextFacts()
        {
            _storage = new Mock<IConsoleStorage>();
            _options = new ConsoleOptions();
        }

        [Fact]
        public void Ctor_ThrowsException_IfConsoleIdIsNull()
        {
            Assert.Throws<ArgumentNullException>("consoleId", () => new ConsoleContext(null, _storage.Object, _options));
        }

        [Fact]
        public void Ctor_ThrowsException_IfStorageIsNull()
        {
            var consoleId = new ConsoleId("1", new DateTime(2016, 1, 1, 0, 0, 0, DateTimeKind.Utc));

            Assert.Throws<ArgumentNullException>("storage", () => new ConsoleContext(consoleId, null, _options));
        }

        [Fact]
        public void Ctor_ThrowsException_IfOptionsIsNull()
        {
            var consoleId = new ConsoleId("1", new DateTime(2016, 1, 1, 0, 0, 0, DateTimeKind.Utc));

            Assert.Throws<ArgumentNullException>("options", () => new ConsoleContext(consoleId, _storage.Object, null));
        }

        [Fact]
        public void Ctor_InitializesConsole()
        {
            var consoleId = new ConsoleId("1", new DateTime(2016, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            var context = new ConsoleContext(consoleId, _storage.Object, _options);

            _storage.Verify(x => x.InitConsole(consoleId));
        }

        [Fact]
        public void AddLine_ThrowsException_IfLineIsNull()
        {
            var consoleId = new ConsoleId("1", new DateTime(2016, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            var context = new ConsoleContext(consoleId, _storage.Object, _options);

            Assert.Throws<ArgumentNullException>("line", () => context.AddLine(null));
        }

        [Fact]
        public void AddLine_ReallyAddsLine()
        {
            var consoleId = new ConsoleId("1", new DateTime(2016, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            var context = new ConsoleContext(consoleId, _storage.Object, _options);

            context.AddLine(new ConsoleLine() { TimeOffset = 0, Message = "line" });

            _storage.Verify(x => x.AddLine(It.IsAny<ConsoleId>(), It.IsAny<ConsoleLine>()));
        }

        [Fact]
        public void AddLine_CorrectsTimeOffset()
        {
            var consoleId = new ConsoleId("1", new DateTime(2016, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            var context = new ConsoleContext(consoleId, _storage.Object, _options);

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
            var context = new ConsoleContext(consoleId, _storage.Object, _options);

            context.WriteLine("line", null);

            _storage.Verify(x => x.AddLine(It.IsAny<ConsoleId>(), It.Is<ConsoleLine>(_ => _.Message == "line" && _.TextColor == null)));
        }

        [Fact]
        public void WriteLine_ReallyAddsLineWithColor()
        {
            var consoleId = new ConsoleId("1", new DateTime(2016, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            var context = new ConsoleContext(consoleId, _storage.Object, _options);

            context.WriteLine("line", ConsoleTextColor.Red);

            _storage.Verify(x => x.AddLine(It.IsAny<ConsoleId>(), It.Is<ConsoleLine>(_ => _.Message == "line" && _.TextColor == ConsoleTextColor.Red)));
        }

        [Fact]
        public void WriteProgressBar_WritesDefaults_AndReturnsNonNull()
        {
            var consoleId = new ConsoleId("1", new DateTime(2016, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            var context = new ConsoleContext(consoleId, _storage.Object, _options);

            var progressBar = context.WriteProgressBar(null, 0, null);

            _storage.Verify(x => x.AddLine(It.IsAny<ConsoleId>(), It.IsAny<ConsoleLine>()));
            Assert.NotNull(progressBar);
        }
        
        [Fact]
        public void WriteProgressBar_WritesName_AndReturnsNonNull()
        {
            var consoleId = new ConsoleId("1", new DateTime(2016, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            var context = new ConsoleContext(consoleId, _storage.Object, _options);

            var progressBar = context.WriteProgressBar("test", 0, null);

            _storage.Verify(x => x.AddLine(It.IsAny<ConsoleId>(), It.Is<ConsoleLine>(_ => _.ProgressName == "test")));
            Assert.NotNull(progressBar);
        }
        
        [Fact]
        public void WriteProgressBar_WritesInitialValue_AndReturnsNonNull()
        {
            var consoleId = new ConsoleId("1", new DateTime(2016, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            var context = new ConsoleContext(consoleId, _storage.Object, _options);

            var progressBar = context.WriteProgressBar(null, 5, null);

            _storage.Verify(x => x.AddLine(It.IsAny<ConsoleId>(), It.Is<ConsoleLine>(_ => _.ProgressValue == 5)));
            Assert.NotNull(progressBar);
        }

        [Fact]
        public void WriteProgressBar_WritesProgressBarColor_AndReturnsNonNull()
        {
            var consoleId = new ConsoleId("1", new DateTime(2016, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            var context = new ConsoleContext(consoleId, _storage.Object, _options);

            var progressBar = context.WriteProgressBar(null, 0, ConsoleTextColor.Red);

            _storage.Verify(x => x.AddLine(It.IsAny<ConsoleId>(), It.Is<ConsoleLine>(_ => _.TextColor == ConsoleTextColor.Red)));
            Assert.NotNull(progressBar);
        }

        [Fact]
        public void Expire_ReallyExpiresLines()
        {
            var consoleId = new ConsoleId("1", new DateTime(2016, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            var context = new ConsoleContext(consoleId, _storage.Object, _options);

            context.Expire(TimeSpan.FromHours(1));

            _storage.Verify(x => x.Expire(It.IsAny<ConsoleId>(), It.IsAny<TimeSpan>()));
        }
        
        [Fact]
        public void FixExpiration_RequestsConsoleTtl_IgnoresIfNegative()
        {
            _storage.Setup(x => x.GetConsoleTtl(It.IsAny<ConsoleId>()))
                .Returns(TimeSpan.FromSeconds(-1));

            var consoleId = new ConsoleId("1", new DateTime(2016, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            var context = new ConsoleContext(consoleId, _storage.Object, _options);

            context.FixExpiration();

            _storage.Verify(x => x.GetConsoleTtl(It.IsAny<ConsoleId>()));
            _storage.Verify(x => x.Expire(It.IsAny<ConsoleId>(), It.IsAny<TimeSpan>()), Times.Never);
        }

        [Fact]
        public void FixExpiration_RequestsConsoleTtl_ExpiresIfPositive()
        {
            _storage.Setup(x => x.GetConsoleTtl(It.IsAny<ConsoleId>()))
                .Returns(TimeSpan.FromHours(1));

            var consoleId = new ConsoleId("1", new DateTime(2016, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            var context = new ConsoleContext(consoleId, _storage.Object, _options);

            context.FixExpiration();

            _storage.Verify(x => x.GetConsoleTtl(It.IsAny<ConsoleId>()));
            _storage.Verify(x => x.Expire(It.IsAny<ConsoleId>(), It.IsAny<TimeSpan>()));
        }
    }
}
