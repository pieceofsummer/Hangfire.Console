using Hangfire.Common;
using Hangfire.Console.Server;
using Hangfire.Server;
using Hangfire.States;
using Hangfire.Storage;
using Moq;
using System;
using System.Collections.Generic;
using Xunit;

namespace Hangfire.Console.Tests.Server
{
    public class ConsoleServerFilterFacts
    {
        private readonly Mock<IServerFilter> _otherFilter;
        private readonly Mock<IJobCancellationToken> _cancellationToken;
        private readonly Mock<JobStorageConnection> _connection;
        private readonly Mock<JobStorageTransaction> _transaction;

        public ConsoleServerFilterFacts()
        {
            _otherFilter = new Mock<IServerFilter>();
            _cancellationToken = new Mock<IJobCancellationToken>();
            _connection = new Mock<JobStorageConnection>();
            _transaction = new Mock<JobStorageTransaction>();

            _connection.Setup(x => x.CreateWriteTransaction())
                .Returns(_transaction.Object);
        }

        [Fact]
        public void DoesNotCreateConsole_IfStateNotFound()
        {
            _connection.Setup(x => x.GetStateData(It.IsAny<string>()))
                .Returns((StateData)null);

            var performer = new BackgroundJobPerformer(CreateJobFilterProvider());
            var context = CreatePerformContext();

            performer.Perform(context);

            var consoleContext = ConsoleContext.FromPerformContext(context);
            Assert.Null(consoleContext);
        }

        [Fact]
        public void DoesNotCreateConsole_IfStateIsNotProcessing()
        {
            _connection.Setup(x => x.GetStateData(It.IsAny<string>()))
                .Returns(CreateState(SucceededState.StateName));

            var performer = new BackgroundJobPerformer(CreateJobFilterProvider());
            var context = CreatePerformContext();

            performer.Perform(context);

            var consoleContext = ConsoleContext.FromPerformContext(context);
            Assert.Null(consoleContext);
        }

        [Fact]
        public void CreatesConsole_IfStateIsProcessing_DoesNotExpireData_IfCancelled()
        {
            _connection.Setup(x => x.GetStateData(It.IsAny<string>()))
                .Returns(CreateState(ProcessingState.StateName));
            _otherFilter.Setup(x => x.OnPerforming(It.IsAny<PerformingContext>()))
                .Callback<PerformingContext>(x => { x.Canceled = true; });

            var performer = new BackgroundJobPerformer(CreateJobFilterProvider());
            var context = CreatePerformContext();

            performer.Perform(context);

            var consoleContext = ConsoleContext.FromPerformContext(context);
            Assert.NotNull(consoleContext);

            _transaction.Verify(x => x.Commit(), Times.Never);
        }

        [Fact]
        public void CreatesConsole_IfStateIsProcessing_DoesNotExpireData_IfConsoleNotPresent()
        {
            _connection.Setup(x => x.GetStateData(It.IsAny<string>()))
                .Returns(CreateState(ProcessingState.StateName));
            _otherFilter.Setup(x => x.OnPerforming(It.IsAny<PerformingContext>()))
                .Callback<PerformingContext>(x => { x.Items.Remove("ConsoleContext"); });

            var performer = new BackgroundJobPerformer(CreateJobFilterProvider());
            var context = CreatePerformContext();

            performer.Perform(context);

            var consoleContext = ConsoleContext.FromPerformContext(context);
            Assert.Null(consoleContext);

            _transaction.Verify(x => x.Commit(), Times.Never);
        }

        [Fact]
        public void CreatesConsole_IfStateIsProcessing_ExpiresData_IfNotCancelled()
        {
            _connection.Setup(x => x.GetStateData(It.IsAny<string>()))
                .Returns(CreateState(ProcessingState.StateName));

            var performer = new BackgroundJobPerformer(CreateJobFilterProvider());
            var context = CreatePerformContext();

            performer.Perform(context);
            
            var consoleContext = ConsoleContext.FromPerformContext(context);
            Assert.NotNull(consoleContext);

            _transaction.Verify(x => x.Commit());
        }

        public static void JobMethod()
        {
        }

        private IJobFilterProvider CreateJobFilterProvider()
        {
            var filters = new JobFilterCollection();
            filters.Add(new ConsoleServerFilter(new ConsoleOptions()));
            filters.Add(_otherFilter.Object);
            return filters;
        }

        private PerformContext CreatePerformContext()
        {
            return new PerformContext(_connection.Object, 
                new BackgroundJob("1", Common.Job.FromExpression(() => JobMethod()), DateTime.UtcNow), 
                _cancellationToken.Object);
        }

        private StateData CreateState(string stateName)
        {
            return new StateData()
            {
                Name = stateName,
                Data = new Dictionary<string, string>()
                {
                    ["StartedAt"] = JobHelper.SerializeDateTime(DateTime.UtcNow)
                }
            };
        }

    }
}
