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
        public void DoesNotCreateConsoleContext_IfStateNotFound()
        {
            _connection.Setup(x => x.GetStateData("1"))
                .Returns((StateData)null);

            var performer = new BackgroundJobPerformer(CreateJobFilterProvider());
            var context = CreatePerformContext();

            performer.Perform(context);

            var consoleContext = ConsoleContext.FromPerformContext(context);
            Assert.Null(consoleContext);
        }

        [Fact]
        public void DoesNotCreateConsoleContext_IfStateIsNotProcessing()
        {
            _connection.Setup(x => x.GetStateData("1"))
                .Returns(CreateState(SucceededState.StateName));

            var performer = new BackgroundJobPerformer(CreateJobFilterProvider());
            var context = CreatePerformContext();

            performer.Perform(context);

            var consoleContext = ConsoleContext.FromPerformContext(context);
            Assert.Null(consoleContext);
        }
        
        [Fact]
        public void CreatesConsoleContext_IfStateIsProcessing_DoesNotExpireData_IfConsoleNotPresent()
        {
            _connection.Setup(x => x.GetStateData("1"))
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
        public void CreatesConsoleContext_IfStateIsProcessing_FixesExpiration_IfFollowsJobRetention()
        {
            _connection.Setup(x => x.GetStateData("1"))
                .Returns(CreateState(ProcessingState.StateName));
            _connection.Setup(x => x.GetHashTtl(It.IsAny<string>()))
                .Returns(TimeSpan.FromSeconds(1));

            var performer = new BackgroundJobPerformer(CreateJobFilterProvider(true));
            var context = CreatePerformContext();

            performer.Perform(context);

            var consoleContext = ConsoleContext.FromPerformContext(context);
            Assert.Null(consoleContext);

            _connection.Verify(x => x.GetHashTtl(It.IsAny<string>()));
            
            _transaction.Verify(x => x.ExpireSet(It.IsAny<string>(), It.IsAny<TimeSpan>()));
            _transaction.Verify(x => x.ExpireHash(It.IsAny<string>(), It.IsAny<TimeSpan>()));
            
            _transaction.Verify(x => x.Commit());
        }

        [Fact]
        public void CreatesConsoleContext_IfStateIsProcessing_DoesNotFixExpiration_IfNegativeTtl_AndFollowsJobRetention()
        {
            _connection.Setup(x => x.GetStateData("1"))
                .Returns(CreateState(ProcessingState.StateName));
            _connection.Setup(x => x.GetHashTtl(It.IsAny<string>()))
                .Returns(TimeSpan.FromSeconds(-1));

            var performer = new BackgroundJobPerformer(CreateJobFilterProvider(true));
            var context = CreatePerformContext();

            performer.Perform(context);

            var consoleContext = ConsoleContext.FromPerformContext(context);
            Assert.Null(consoleContext);

            _connection.Verify(x => x.GetHashTtl(It.IsAny<string>()));

            _transaction.Verify(x => x.Commit(), Times.Never);
        }

        [Fact]
        public void CreatesConsoleContext_IfStateIsProcessing_DoesNotFixExpiration_IfZeroTtl_AndFollowsJobRetention()
        {
            _connection.Setup(x => x.GetStateData("1"))
                .Returns(CreateState(ProcessingState.StateName));
            _connection.Setup(x => x.GetHashTtl(It.IsAny<string>()))
                .Returns(TimeSpan.Zero);

            var performer = new BackgroundJobPerformer(CreateJobFilterProvider(true));
            var context = CreatePerformContext();

            performer.Perform(context);

            var consoleContext = ConsoleContext.FromPerformContext(context);
            Assert.Null(consoleContext);

            _connection.Verify(x => x.GetHashTtl(It.IsAny<string>()));

            _transaction.Verify(x => x.Commit(), Times.Never);
        }

        [Fact]
        public void CreatesConsoleContext_IfStateIsProcessing_ExpiresData_IfNotFollowsJobRetention()
        {
            _connection.Setup(x => x.GetStateData("1"))
                .Returns(CreateState(ProcessingState.StateName));

            var performer = new BackgroundJobPerformer(CreateJobFilterProvider());
            var context = CreatePerformContext();

            performer.Perform(context);
            
            var consoleContext = ConsoleContext.FromPerformContext(context);
            Assert.Null(consoleContext);

            _connection.Verify(x => x.GetHashTtl(It.IsAny<string>()), Times.Never);

            _transaction.Verify(x => x.ExpireSet(It.IsAny<string>(), It.IsAny<TimeSpan>()));
            _transaction.Verify(x => x.ExpireHash(It.IsAny<string>(), It.IsAny<TimeSpan>()));

            _transaction.Verify(x => x.Commit());
        }

        public static void JobMethod(PerformContext context)
        {
            // reset transaction method calls after OnPerforming is completed
            var @this = (ConsoleServerFilterFacts) context.Items["this"];
            @this._transaction.ResetCalls();
        }

        private IJobFilterProvider CreateJobFilterProvider(bool followJobRetention = false)
        {
            var filters = new JobFilterCollection();
            filters.Add(new ConsoleServerFilter(new ConsoleOptions() { FollowJobRetentionPolicy = followJobRetention }));
            filters.Add(_otherFilter.Object);
            return new JobFilterProviderCollection(filters);
        }

        private PerformContext CreatePerformContext()
        {
            var context = new PerformContext(_connection.Object, 
                new BackgroundJob("1", Job.FromExpression(() => JobMethod(null)), DateTime.UtcNow), 
                _cancellationToken.Object);
            context.Items["this"] = this;
            return context;
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
