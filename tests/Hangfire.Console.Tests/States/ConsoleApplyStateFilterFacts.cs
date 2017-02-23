using Hangfire.Common;
using Hangfire.Console.States;
using Hangfire.States;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Hangfire.Console.Tests.States
{
    public class ConsoleApplyStateFilterFacts
    {
        private readonly Mock<IApplyStateFilter> _otherFilter;
        private readonly Mock<JobStorage> _storage;
        private readonly Mock<JobStorageConnection> _connection;
        private readonly Mock<JobStorageTransaction> _transaction;
        private readonly Mock<IMonitoringApi> _monitoring;

        public ConsoleApplyStateFilterFacts()
        {
            _otherFilter = new Mock<IApplyStateFilter>();
            _storage = new Mock<JobStorage>();
            _connection = new Mock<JobStorageConnection>();
            _transaction = new Mock<JobStorageTransaction>();
            _monitoring = new Mock<IMonitoringApi>();

            _storage.Setup(x => x.GetConnection())
                .Returns(_connection.Object);
            _storage.Setup(x => x.GetMonitoringApi())
                .Returns(_monitoring.Object);

            _connection.Setup(x => x.CreateWriteTransaction())
                .Returns(_transaction.Object);
        }

        [Fact]
        public void UsesFinalJobExpirationTimeoutValue()
        {
            _otherFilter.Setup(x => x.OnStateApplied(It.IsAny<ApplyStateContext>(), It.IsAny<IWriteOnlyTransaction>()))
                .Callback<ApplyStateContext, IWriteOnlyTransaction>((c, t) => c.JobExpirationTimeout = TimeSpan.FromSeconds(123));
            _connection.Setup(x => x.GetJobData("1"))
                .Returns(CreateJobData(ProcessingState.StateName));
            _monitoring.Setup(x => x.JobDetails("1"))
                .Returns(CreateJobDetails());

            var stateChanger = new BackgroundJobStateChanger(CreateJobFilterProvider());
            var context = CreateStateChangeContext(new MockSucceededState());

            stateChanger.ChangeState(context);

            _transaction.Verify(x => x.ExpireJob(It.IsAny<string>(), TimeSpan.FromSeconds(123)));
            _transaction.Verify(x => x.ExpireSet(It.IsAny<string>(), TimeSpan.FromSeconds(123)));
        }
        
        [Fact]
        public void DoesNotExpire_IfNotFollowsJobRetention()
        {
            _connection.Setup(x => x.GetJobData("1"))
                .Returns(CreateJobData(ProcessingState.StateName));
            _monitoring.Setup(x => x.JobDetails("1"))
                .Returns(CreateJobDetails());
            
            var stateChanger = new BackgroundJobStateChanger(CreateJobFilterProvider(false));
            var context = CreateStateChangeContext(new MockSucceededState());
            
            stateChanger.ChangeState(context);

            _transaction.Verify(x => x.ExpireSet(It.IsAny<string>(), It.IsAny<TimeSpan>()), Times.Never);
            _transaction.Verify(x => x.ExpireHash(It.IsAny<string>(), It.IsAny<TimeSpan>()), Times.Never);
        }

        [Fact]
        public void Expires_IfStateIsFinal()
        {
            _connection.Setup(x => x.GetJobData("1"))
                .Returns(CreateJobData(ProcessingState.StateName));
            _monitoring.Setup(x => x.JobDetails("1"))
                .Returns(CreateJobDetails());

            var stateChanger = new BackgroundJobStateChanger(CreateJobFilterProvider());
            var context = CreateStateChangeContext(new MockSucceededState());

            stateChanger.ChangeState(context);

            _transaction.Verify(x => x.ExpireSet(It.IsAny<string>(), It.IsAny<TimeSpan>()));
            _transaction.Verify(x => x.ExpireHash(It.IsAny<string>(), It.IsAny<TimeSpan>()));
        }
        
        [Fact]
        public void Persists_IfStateIsNotFinal()
        {
            _connection.Setup(x => x.GetJobData("1"))
                .Returns(CreateJobData(ProcessingState.StateName));
            _monitoring.Setup(x => x.JobDetails("1"))
                .Returns(CreateJobDetails());

            var stateChanger = new BackgroundJobStateChanger(CreateJobFilterProvider());
            var context = CreateStateChangeContext(new MockFailedState());
            
            stateChanger.ChangeState(context);

            _transaction.Verify(x => x.PersistSet(It.IsAny<string>()));
            _transaction.Verify(x => x.PersistHash(It.IsAny<string>()));
        }

        private IJobFilterProvider CreateJobFilterProvider(bool followJobRetention = true)
        {
            var filters = new JobFilterCollection();
            filters.Add(new ConsoleApplyStateFilter(new ConsoleOptions() { FollowJobRetentionPolicy = followJobRetention }), int.MaxValue);
            filters.Add(_otherFilter.Object);
            return new JobFilterProviderCollection(filters);
        }

        public class MockSucceededState : IState
        {
            public string Name => SucceededState.StateName;

            public string Reason => null;

            public bool IsFinal => true;

            public bool IgnoreJobLoadException => false;
            
            public Dictionary<string, string> SerializeData()
            {
                return new Dictionary<string, string>();
            }
        }

        public class MockFailedState : IState
        {
            public string Name => FailedState.StateName;

            public string Reason => null;

            public bool IsFinal => false;

            public bool IgnoreJobLoadException => false;

            public Dictionary<string, string> SerializeData()
            {
                return new Dictionary<string, string>();
            }
        }

        private StateChangeContext CreateStateChangeContext(IState state)
        {
            return new StateChangeContext(_storage.Object, _connection.Object, "1", state);
        }

        public static void JobMethod()
        {
        }
        
        private JobDetailsDto CreateJobDetails()
        {
            var date = DateTime.UtcNow.AddHours(-1);
            var history = new List<StateHistoryDto>();

            history.Add(new StateHistoryDto()
            {
                StateName = EnqueuedState.StateName,
                CreatedAt = date,
                Data = new Dictionary<string, string>()
                {
                    ["EnqueuedAt"] = JobHelper.SerializeDateTime(date),
                    ["Queue"] = EnqueuedState.DefaultQueue
                }
            });

            history.Add(new StateHistoryDto()
            {
                StateName = ProcessingState.StateName,
                CreatedAt = date.AddSeconds(2),
                Data = new Dictionary<string, string>()
                {
                    ["StartedAt"] = JobHelper.SerializeDateTime(date.AddSeconds(2)),
                    ["ServerId"] = "SERVER-1",
                    ["WorkerId"] = "WORKER-1"
                }
            });

            history.Reverse();
            
            return new JobDetailsDto()
            {
                CreatedAt = history[0].CreatedAt,
                Job = Job.FromExpression(() => JobMethod()),
                History = history
            };
        }

        private JobData CreateJobData(string state)
        {
            return new JobData()
            {
                CreatedAt = DateTime.UtcNow.AddHours(-1),
                Job = Job.FromExpression(() => JobMethod()),
                State = state
            };
        }
    }
}
