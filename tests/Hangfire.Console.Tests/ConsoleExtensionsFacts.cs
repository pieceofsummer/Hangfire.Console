using Hangfire.Console.Progress;
using Hangfire.Console.Serialization;
using Hangfire.Console.Server;
using Hangfire.Console.Storage;
using Hangfire.Server;
using Hangfire.Storage;
using Moq;
using System;
using Xunit;

namespace Hangfire.Console.Tests
{
    public class ConsoleExtensionsFacts
    {
        private readonly Mock<IJobCancellationToken> _cancellationToken;
        private readonly Mock<JobStorageConnection> _connection;
        private readonly Mock<IWriteOnlyTransaction> _transaction;

        public ConsoleExtensionsFacts()
        {
            _cancellationToken = new Mock<IJobCancellationToken>();
            _connection = new Mock<JobStorageConnection>();
            _transaction = new Mock<IWriteOnlyTransaction>();

            _connection.Setup(x => x.CreateWriteTransaction())
                .Returns(_transaction.Object);
        }
        
        [Fact]
        public void WriteLine_DoesNotFail_IfContextIsNull()
        {
            ConsoleExtensions.WriteLine(null, "");

            _transaction.Verify(x => x.Commit(), Times.Never);
        }

        [Fact]
        public void WriteLine_Writes_IfConsoleCreated()
        {
            var context = CreatePerformContext();
            context.Items["ConsoleContext"] = CreateConsoleContext(context);

            ConsoleExtensions.WriteLine(context, "");

            _transaction.Verify(x => x.Commit());
        }

        [Fact]
        public void WriteLine_DoesNotFail_IfConsoleNotCreated()
        {
            var context = CreatePerformContext();

            ConsoleExtensions.WriteLine(context, "");

            _transaction.Verify(x => x.Commit(), Times.Never);
        }

        [Fact]
        public void WriteProgressBar_ReturnsNoOp_IfContextIsNull()
        {
            var progressBar = ConsoleExtensions.WriteProgressBar(null, 0);

            Assert.IsType<NoOpProgressBar>(progressBar);
            _transaction.Verify(x => x.Commit(), Times.Never);
        }

        [Fact]
        public void WriteProgressBar_ReturnsProgressBar_IfConsoleCreated()
        {
            var context = CreatePerformContext();
            context.Items["ConsoleContext"] = CreateConsoleContext(context);

            var progressBar = ConsoleExtensions.WriteProgressBar(context, 0);
            
            Assert.IsType<DefaultProgressBar>(progressBar);
            _transaction.Verify(x => x.Commit());
        }

        [Fact]
        public void WriteProgressBar_ReturnsNoOp_IfConsoleNotCreated()
        {
            var context = CreatePerformContext();

            var progressBar = ConsoleExtensions.WriteProgressBar(context, 0);

            Assert.IsType<NoOpProgressBar>(progressBar);
            _transaction.Verify(x => x.Commit(), Times.Never);
        }

        [Fact]
        public void WriteProgressBarMaxValue_ReturnsNoOp_IfContextIsNull()
        {
            var progressBar = ConsoleExtensions.WriteProgressBar(null, 0.0);

            Assert.IsType<NoOpProgressBar>(progressBar);
            _transaction.Verify(x => x.Commit(), Times.Never);
        }

        [Fact]
        public void WriteProgressBar_ReturnsProgressBarMaxValue_IfConsoleCreated()
        {
            var context = CreatePerformContext();
            context.Items["ConsoleContext"] = CreateConsoleContext(context);

            var progressBar = ConsoleExtensions.WriteProgressBar(context, 0.0, 100.0);

            Assert.IsType<DefaultProgressBar>(progressBar);
            _transaction.Verify(x => x.Commit());
        }

        [Fact]
        public void WriteProgressBarMaxValue_ReturnsNoOp_IfConsoleNotCreated()
        {
            var context = CreatePerformContext();

            var progressBar = ConsoleExtensions.WriteProgressBar(context, 0.0);

            Assert.IsType<NoOpProgressBar>(progressBar);
            _transaction.Verify(x => x.Commit(), Times.Never);
        }

        public static void JobMethod()
        {
        }

        private PerformContext CreatePerformContext()
        {
            return new PerformContext(_connection.Object,
                new BackgroundJob("1", Common.Job.FromExpression(() => JobMethod()), DateTime.UtcNow),
                _cancellationToken.Object);
        }

        private ConsoleContext CreateConsoleContext(PerformContext context)
        {
            return new ConsoleContext(
                new ConsoleId(context.BackgroundJob.Id, DateTime.UtcNow),
                new ConsoleStorage(context.Connection));
        }

    }
}
