using Hangfire.Console.Serialization;
using Hangfire.Console.Tests.Mocks;
using Hangfire.Server;
using Hangfire.Storage;
using Moq;
using System;
using Xunit;

namespace Hangfire.Console.Tests
{
    public class ConsoleExtensionsFacts
    {
        private readonly Mock<IStorageConnection> _connection;
        private readonly Mock<IWriteOnlyTransaction> _transaction;

        public ConsoleExtensionsFacts()
        {
            _connection = new Mock<IStorageConnection>();
            _transaction = new Mock<IWriteOnlyTransaction>();

            _connection.Setup(x => x.CreateWriteTransaction())
                .Returns(_transaction.Object);
        }

        [Fact]
        public void AddLine_ReallyAddsLine()
        {
            var consoleId = new ConsoleId("1", new DateTime(2016, 1, 1, 0, 0, 0, DateTimeKind.Utc));

            var context = CreatePerformContext(_connection.Object);
            ConsoleExtensions.AddLine(context, consoleId, new ConsoleLine() { TimeOffset = 0, Message = "line" });

            _transaction.Verify(x => x.AddToSet(It.IsAny<string>(), It.IsAny<string>()));
        }

        [Fact]
        public void AddLine_CorrectsDuplicateTimeOffset()
        {
            var consoleId = new ConsoleId("1", new DateTime(2016, 1, 1, 0, 0, 0, DateTimeKind.Utc));

            var context = CreatePerformContext(_connection.Object);
            ConsoleExtensions.AddLine(context, consoleId, new ConsoleLine() { TimeOffset = 0, Message = "line" });

            var line2 = new ConsoleLine() { TimeOffset = 0, Message = "line" };
            ConsoleExtensions.AddLine(context, consoleId, line2);

            _transaction.Verify(x => x.AddToSet(It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(2));
            Assert.NotEqual(0, line2.TimeOffset, 4);
        }

        [Fact]
        public void SetTextColor_ThrowsException_IfContextIsNull()
        {
            Assert.Throws<ArgumentNullException>("context", () => ConsoleExtensions.SetTextColor(null, ConsoleTextColor.White));
        }

        [Fact]
        public void SetTextColor_ThrowsException_IfColorIsNull()
        {
            var context = CreatePerformContext(_connection.Object);

            Assert.Throws<ArgumentNullException>("color", () => ConsoleExtensions.SetTextColor(context, null));
        }

        [Fact]
        public void SetTextColor_ReallySetsTextColor()
        {
            var context = CreatePerformContext(_connection.Object);

            ConsoleExtensions.SetTextColor(context, ConsoleTextColor.Yellow);

            Assert.True(context.Items.ContainsKey("ConsoleTextColor"));
            Assert.Equal(ConsoleTextColor.Yellow, context.Items["ConsoleTextColor"]);
        }

        [Fact]
        public void ResetTextColor_ThrowsException_IfContextIsNull()
        {
            Assert.Throws<ArgumentNullException>("context", () => ConsoleExtensions.ResetTextColor(null));
        }

        [Fact]
        public void ResetTextColor_ReallyResetsTextColor()
        {
            var context = CreatePerformContext(_connection.Object);
            context.Items["ConsoleTextColor"] = ConsoleTextColor.Black;

            ConsoleExtensions.ResetTextColor(context);

            Assert.False(context.Items.ContainsKey("ConsoleTextColor"));
        }
        
        [Fact]
        public void WriteLine_ThrowsException_IfContextIsNull()
        {
            Assert.Throws<ArgumentNullException>("context", () => ConsoleExtensions.WriteLine(null, ""));
        }

        [Fact]
        public void WriteLine_DoesNothing_IfConsoleNotCreated()
        {
            var context = CreatePerformContext(_connection.Object);

            ConsoleExtensions.WriteLine(context, "");

            _transaction.Verify(x => x.AddToSet(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void WriteLine_ReallyAddsLine()
        {
            var consoleId = new ConsoleId("1", DateTime.UtcNow);

            var context = CreatePerformContext(_connection.Object);
            context.Items["ConsoleId"] = consoleId;

            ConsoleExtensions.WriteLine(context, "");

            _transaction.Verify(x => x.AddToSet(It.Is<string>(s => s == consoleId.ToString()), It.IsAny<string>()));
        }

        [Fact]
        public void WriteProgressBar_ThrowsException_IfContextIsNull()
        {
            Assert.Throws<ArgumentNullException>("context", () => ConsoleExtensions.WriteProgressBar(null));
        }

        [Fact]
        public void WriteProgressBar_ReturnsNoOp_IfConsoleNotCreated()
        {
            var context = CreatePerformContext(_connection.Object);

            var progress = ConsoleExtensions.WriteProgressBar(context);

            Assert.NotNull(progress);
            _transaction.Verify(x => x.AddToSet(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void WriteProgressBar_ReturnsNotNull_AndWritesDefaultValue()
        {
            var consoleId = new ConsoleId("1", DateTime.UtcNow);

            var context = CreatePerformContext(_connection.Object);
            context.Items["ConsoleId"] = consoleId;

            var progress = ConsoleExtensions.WriteProgressBar(context);

            Assert.NotNull(progress);
            _transaction.Verify(x => x.AddToSet(It.Is<string>(s => s == consoleId.ToString()), It.IsAny<string>()));
        }

        public static void JobMethod()
        {
        }

        private PerformContext CreatePerformContext(IStorageConnection connection)
        {
            return new PerformContext(connection,
                new BackgroundJob("1", Common.Job.FromExpression(() => JobMethod()), DateTime.UtcNow),
                new MockCancellationToken());
        }

    }
}
