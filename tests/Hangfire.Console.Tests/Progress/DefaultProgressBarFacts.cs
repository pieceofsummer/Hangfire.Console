using Hangfire.Console.Serialization;
using Hangfire.Console.Progress;
using Moq;
using System;
using Xunit;
using Hangfire.Console.Server;
using Hangfire.Console.Storage;

namespace Hangfire.Console.Tests.Progress
{
    public class DefaultProgressBarFacts
    {
        private readonly Mock<IConsoleStorage> _storage;

        public DefaultProgressBarFacts()
        {
            _storage = new Mock<IConsoleStorage>();
        }

        private ConsoleContext CreateConsoleContext(ConsoleOptions options = null)
        {
            return new ConsoleContext(
                new ConsoleId("1", DateTime.UtcNow),
                _storage.Object,
                options ?? new ConsoleOptions());
        }

        [Fact]
        public void Ctor_ThrowsException_IfContextIsNull()
        {
            Assert.Throws<ArgumentNullException>("context", () => new DefaultProgressBar(null, "test", 1, null, null));
        }

        [Fact]
        public void Ctor_ThrowsException_IfProgressBarIdIsNull()
        {
            Assert.Throws<ArgumentNullException>("progressBarId", () => new DefaultProgressBar(CreateConsoleContext(), null, 1, null, null));
        }

        [Fact]
        public void Ctor_ThrowsException_IfProgressBarIdIsEmpty()
        {
            Assert.Throws<ArgumentNullException>("progressBarId", () => new DefaultProgressBar(CreateConsoleContext(), "", 1, null, null));
        }

        [Fact]
        public void Ctor_ThrowsException_IfDecimalDigitsIsLessThanZero()
        {
            Assert.Throws<ArgumentOutOfRangeException>("decimalDigits", () => new DefaultProgressBar(CreateConsoleContext(), "1", -1, null, null));
        }

        [Fact]
        public void Ctor_ThrowsException_IfDecimalDigitsIsMoreThanThree()
        {
            Assert.Throws<ArgumentOutOfRangeException>("decimalDigits", () => new DefaultProgressBar(CreateConsoleContext(), "1", 4, null, null));
        }

        [Fact]
        public void SetValue_ThrowsException_IfValueIsOutOfRange()
        {
            var progressBar = new DefaultProgressBar(CreateConsoleContext(), "1", 1, null, null);

            // check for too small values
            Assert.Throws<ArgumentOutOfRangeException>("value", () => progressBar.SetValue(-1));
            Assert.Throws<ArgumentOutOfRangeException>("value", () => progressBar.SetValue(-0.1));
            
            // check for too big values
            Assert.Throws<ArgumentOutOfRangeException>("value", () => progressBar.SetValue(101));
            Assert.Throws<ArgumentOutOfRangeException>("value", () => progressBar.SetValue(100.1));
        }

        [Fact]
        public void SetValue_DoesNotSetValueMultipleTimes()
        {
            var progressBar = new DefaultProgressBar(CreateConsoleContext(), "1", 1, null, null);

            progressBar.SetValue(1);
            progressBar.SetValue(1);

            _storage.Verify(x => x.AddLine(It.IsAny<ConsoleId>(), It.IsAny<ConsoleLine>()), Times.Once);
        }

        [Fact]
        public void SetValue_SetsNameOnlyOnce()
        {
            var progressBar = new DefaultProgressBar(CreateConsoleContext(), "1", 1, "name", null);

            progressBar.SetValue(1);
            progressBar.SetValue(2);
            progressBar.SetValue(3);

            _storage.Verify(x => x.AddLine(It.IsAny<ConsoleId>(), It.IsAny<ConsoleLine>()), Times.AtLeast(2));
            _storage.Verify(x => x.AddLine(It.IsAny<ConsoleId>(), It.Is<ConsoleLine>(l => l.ProgressName == "name")), Times.Once);
        }

        [Fact]
        public void SetValue_SetsColorOnlyOnce()
        {
            var progressBar = new DefaultProgressBar(CreateConsoleContext(), "1", 1, null, "color");

            progressBar.SetValue(1);
            progressBar.SetValue(2);
            progressBar.SetValue(3);

            _storage.Verify(x => x.AddLine(It.IsAny<ConsoleId>(), It.IsAny<ConsoleLine>()), Times.AtLeast(2));
            _storage.Verify(x => x.AddLine(It.IsAny<ConsoleId>(), It.Is<ConsoleLine>(l => l.TextColor == "color")), Times.Once);
        }

        [Theory]
        [InlineData(0, 101)]
        [InlineData(1, 1001)]
        [InlineData(2, 10001)]
        [InlineData(3, 100001)]
        public void SetValue_RespectsDecimalDigits(int decimalDigits, int expectedWrites)
        {
            var progressBar = new DefaultProgressBar(CreateConsoleContext(), "1", decimalDigits, null, null);

            for (int i = 0; i <= 100_000; i++)
            {
                progressBar.SetValue(i / 1000.0);
            }

            _storage.Verify(x => x.AddLine(It.IsAny<ConsoleId>(), It.IsAny<ConsoleLine>()), Times.Exactly(expectedWrites));
        }
    }
}