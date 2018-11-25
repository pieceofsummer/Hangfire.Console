using Hangfire.Common;
using Hangfire.Console.Server;
using Hangfire.Server;
using Hangfire.States;
using Hangfire.Storage;
using Moq;
using System;
using System.Collections.Generic;
using Hangfire.Console.Runtime;
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
            _connection.Setup(x => x.GetStateData("1"))
                .Returns(CreateState(ProcessingState.StateName));
            
            JobStorageInfo.MockSetup(_connection.Object, _transaction.Object);
        }
        
        [Fact]
        public void OnPerforming_InterruptsProcessing_IfConnectionIsNotJobStorageConnection()
        {
            var connection = new Mock<IStorageConnection>();
            
            connection.Setup(x => x.CreateWriteTransaction())
                .Returns(_transaction.Object);
            connection.Setup(x => x.GetStateData("1"))
                .Returns(CreateState(ProcessingState.StateName));
            
            JobStorageInfo.MockSetup(connection.Object, _transaction.Object);
            
            var performer = new BackgroundJobPerformer(CreateJobFilterProvider());
            var context = CreatePerformContext(connection.Object, _transaction.Invocations);

            performer.Perform(context);
            
            connection.Verify(x => x.GetStateData(It.IsAny<string>()), Times.Never);
            connection.Verify(x => x.CreateWriteTransaction(), Times.Never);
        }
        
        [Fact]
        public void OnPerforming_InterruptsProcessing_IfTransactionIsNotJobStorageTransaction()
        {
            var transaction = new Mock<IWriteOnlyTransaction>();
            
            _connection.Setup(x => x.CreateWriteTransaction())
                .Returns(transaction.Object);
            _connection.Setup(x => x.GetHashTtl(It.IsAny<string>()))
                .Returns(TimeSpan.FromSeconds(1));
            
            JobStorageInfo.MockSetup(_connection.Object, transaction.Object);
            
            var performer = new BackgroundJobPerformer(CreateJobFilterProvider());
            var context = CreatePerformContext(_connection.Object, transaction.Invocations);

            performer.Perform(context);
            
            _connection.Verify(x => x.GetStateData(It.IsAny<string>()), Times.Never);
            _connection.Verify(x => x.CreateWriteTransaction(), Times.Never);
        }
        
        [Fact]
        public void OnPerforming_DoesNotCreateConsoleContext_IfStateNotFound()
        {
            _connection.Setup(x => x.GetStateData("1"))
                .Returns((StateData)null);
            
            var performer = new BackgroundJobPerformer(CreateJobFilterProvider());
            var context = CreatePerformContext();

            performer.Perform(context);
            
            _connection.Verify(x => x.GetStateData("1"));
            _connection.Verify(x => x.CreateWriteTransaction(), Times.Never);
        }

        [Fact]
        public void OnPerforming_DoesNotCreateConsoleContext_IfStateIsNotProcessing()
        {
            _connection.Setup(x => x.GetStateData("1"))
                .Returns(CreateState(SucceededState.StateName));

            var performer = new BackgroundJobPerformer(CreateJobFilterProvider());
            var context = CreatePerformContext();

            performer.Perform(context);
            
            _connection.Verify(x => x.GetStateData("1"));
            _connection.Verify(x => x.CreateWriteTransaction(), Times.Never);
        }
        
        [Fact]
        public void OnPerforming_CreatesConsoleContext_IfStateIsProcessing__OnPerformed_DoesNotExpireData_IfConsoleNotPresent()
        {
            _otherFilter.Setup(x => x.OnPerforming(It.IsAny<PerformingContext>()))
                .Callback<PerformingContext>(x => { x.Items.Remove(ConsoleContext.Key); });

            var performer = new BackgroundJobPerformer(CreateJobFilterProvider());
            var context = CreatePerformContext();

            performer.Perform(context);
            
            _connection.Verify(x => x.GetStateData("1"));
            _transaction.Verify(x => x.Commit(), Times.Never);
        }

        [Fact]
        public void OnPerforming_CreatesConsoleContext_IfStateIsProcessing__OnPerformed_FixesExpiration_IfFollowsJobRetention()
        {
            _connection.Setup(x => x.GetHashTtl(It.IsAny<string>()))
                .Returns(TimeSpan.FromSeconds(1));

            var performer = new BackgroundJobPerformer(CreateJobFilterProvider(true));
            var context = CreatePerformContext();

            performer.Perform(context);
            
            _connection.Verify(x => x.GetHashTtl(It.IsAny<string>()));
            _transaction.Verify(x => x.ExpireSet(It.IsAny<string>(), It.IsAny<TimeSpan>()));
            _transaction.Verify(x => x.ExpireHash(It.IsAny<string>(), It.IsAny<TimeSpan>()));
            _transaction.Verify(x => x.Commit());
        }

        [Fact]
        public void OnPerforming_CreatesConsoleContext_IfStateIsProcessing__OnPerformed_DoesNotFixExpiration_IfNegativeTtl_AndFollowsJobRetention()
        {
            _connection.Setup(x => x.GetHashTtl(It.IsAny<string>()))
                .Returns(TimeSpan.FromSeconds(-1));

            var performer = new BackgroundJobPerformer(CreateJobFilterProvider(true));
            var context = CreatePerformContext();

            performer.Perform(context);

            _connection.Verify(x => x.GetHashTtl(It.IsAny<string>()));
            _transaction.Verify(x => x.Commit(), Times.Never);
        }

        [Fact]
        public void OnPerforming_CreatesConsoleContext_IfStateIsProcessing__OnPerformed_DoesNotFixExpiration_IfZeroTtl_AndFollowsJobRetention()
        {
            _connection.Setup(x => x.GetHashTtl(It.IsAny<string>()))
                .Returns(TimeSpan.Zero);

            var performer = new BackgroundJobPerformer(CreateJobFilterProvider(true));
            var context = CreatePerformContext();

            performer.Perform(context);
            
            _connection.Verify(x => x.GetHashTtl(It.IsAny<string>()));
            _transaction.Verify(x => x.Commit(), Times.Never);
        }

        [Fact]
        public void OnPerforming_CreatesConsoleContext_IfStateIsProcessing__OnPerformed_ExpiresData_IfNotFollowsJobRetention()
        {
            var performer = new BackgroundJobPerformer(CreateJobFilterProvider());
            var context = CreatePerformContext();

            performer.Perform(context);
            
            _connection.Verify(x => x.GetHashTtl(It.IsAny<string>()), Times.Never);
            _transaction.Verify(x => x.ExpireSet(It.IsAny<string>(), It.IsAny<TimeSpan>()));
            _transaction.Verify(x => x.ExpireHash(It.IsAny<string>(), It.IsAny<TimeSpan>()));
            _transaction.Verify(x => x.Commit());
        }

        private static class JobClass
        {
            public static void JobMethod(PerformContext context)
            {
                // reset transaction method calls after OnPerforming is completed
                (context.Items["reset"] as IInvocationList)?.Clear();
            }
        }

        private IJobFilterProvider CreateJobFilterProvider(bool followJobRetention = false)
        {
            var filters = new JobFilterCollection();
            filters.Add(new ConsoleServerFilter(new ConsoleOptions() { FollowJobRetentionPolicy = followJobRetention }));
            filters.Add(_otherFilter.Object);
            return new JobFilterProviderCollection(filters);
        }

        private PerformContext CreatePerformContext() => CreatePerformContext(_connection.Object, _transaction.Invocations);

        private PerformContext CreatePerformContext(IStorageConnection connection, IInvocationList invocationList = null)
        {
            var context = new PerformContext(connection, 
                new BackgroundJob("1", Job.FromExpression(() => JobClass.JobMethod(null)), DateTime.UtcNow), 
                _cancellationToken.Object);
            context.Items["reset"] = invocationList;
            return context;
        }

        private StateData CreateState(string stateName)
        {
            return new StateData()
            {
                Name = stateName,
                Data = new Dictionary<string, string>()
                {
                    ["StartedAt"] = JobHelper.SerializeDateTime(DateTime.UtcNow),
                    ["ServerId"] = "SERVER-1",
                    ["WorkerId"] = "WORKER-2"
                }
            };
        }

    }
}
