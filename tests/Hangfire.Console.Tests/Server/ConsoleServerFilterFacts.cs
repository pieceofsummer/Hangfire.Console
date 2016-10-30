using Hangfire.Common;
using Hangfire.Console.Serialization;
using Hangfire.Console.Server;
using Hangfire.Console.Tests.Mocks;
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
        private readonly Mock<IStorageConnection> _connection;
        private readonly Mock<JobStorageTransaction> _transaction;

        public ConsoleServerFilterFacts()
        {
            _connection = new Mock<IStorageConnection>();
            _otherFilter = new Mock<IServerFilter>();
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

            Assert.False(context.Items.ContainsKey("ConsoleId"));
        }

        [Fact]
        public void DoesNotCreateConsole_IfStateIsNotProcessing()
        {
            _connection.Setup(x => x.GetStateData(It.IsAny<string>()))
                .Returns(CreateState(SucceededState.StateName));

            var performer = new BackgroundJobPerformer(CreateJobFilterProvider());
            var context = CreatePerformContext();

            performer.Perform(context);

            Assert.False(context.Items.ContainsKey("ConsoleId"));
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

            Assert.True(context.Items.ContainsKey("ConsoleId"));
            Assert.IsType<ConsoleId>(context.Items["ConsoleId"]);

            var consoleId = (ConsoleId)context.Items["ConsoleId"];
            Assert.Equal("1", consoleId.JobId);

            _transaction.Verify(x => x.ExpireSet(It.IsAny<string>(), It.IsAny<TimeSpan>()), Times.Never);
        }

        [Fact]
        public void CreatesConsole_IfStateIsProcessing_DoesNotExpireData_IfConsoleNotPresent()
        {
            _connection.Setup(x => x.GetStateData(It.IsAny<string>()))
                .Returns(CreateState(ProcessingState.StateName));
            _otherFilter.Setup(x => x.OnPerforming(It.IsAny<PerformingContext>()))
                .Callback<PerformingContext>(x => { x.Items.Remove("ConsoleId"); });

            var performer = new BackgroundJobPerformer(CreateJobFilterProvider());
            var context = CreatePerformContext();

            performer.Perform(context);

            Assert.False(context.Items.ContainsKey("ConsoleId"));

            _transaction.Verify(x => x.ExpireSet(It.IsAny<string>(), It.IsAny<TimeSpan>()), Times.Never);
        }

        [Fact]
        public void CreatesConsole_IfStateIsProcessing_ExpiresData_IfNotCancelled()
        {
            _connection.Setup(x => x.GetStateData(It.IsAny<string>()))
                .Returns(CreateState(ProcessingState.StateName));

            var performer = new BackgroundJobPerformer(CreateJobFilterProvider());
            var context = CreatePerformContext();

            performer.Perform(context);

            Assert.True(context.Items.ContainsKey("ConsoleId"));
            Assert.IsType<ConsoleId>(context.Items["ConsoleId"]);

            var consoleId = (ConsoleId)context.Items["ConsoleId"];
            Assert.Equal("1", consoleId.JobId);

            _transaction.Verify(x => x.ExpireSet(It.Is<string>(y => y == consoleId.ToString()), It.IsAny<TimeSpan>()));
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
                new MockCancellationToken());
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
