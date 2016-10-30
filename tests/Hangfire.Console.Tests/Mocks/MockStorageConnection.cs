using Hangfire.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using Hangfire.Common;
using Hangfire.Server;
using System.Threading;

namespace Hangfire.Console.Tests.Mocks
{
    public class MockStorageConnection : JobStorageConnection
    {
        public List<MockSetEntry> Sets { get; } = new List<MockSetEntry>();

        public List<MockSetEntry> Lists { get; } = new List<MockSetEntry>();

        public StateData StateData { get; set; }

        #region Not implemented

        public override IDisposable AcquireDistributedLock(string resource, TimeSpan timeout)
        {
            throw new NotImplementedException();
        }

        public override void AnnounceServer(string serverId, ServerContext context)
        {
            throw new NotImplementedException();
        }

        public override string CreateExpiredJob(Job job, IDictionary<string, string> parameters, DateTime createdAt, TimeSpan expireIn)
        {
            throw new NotImplementedException();
        }
        
        public override IFetchedJob FetchNextJob(string[] queues, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public override Dictionary<string, string> GetAllEntriesFromHash(string key)
        {
            throw new NotImplementedException();
        }
        
        public override string GetFirstByLowestScoreFromSet(string key, double fromScore, double toScore)
        {
            throw new NotImplementedException();
        }

        public override JobData GetJobData(string jobId)
        {
            throw new NotImplementedException();
        }

        public override string GetJobParameter(string id, string name)
        {
            throw new NotImplementedException();
        }
        
        public override void Heartbeat(string serverId)
        {
            throw new NotImplementedException();
        }

        public override void RemoveServer(string serverId)
        {
            throw new NotImplementedException();
        }

        public override int RemoveTimedOutServers(TimeSpan timeOut)
        {
            throw new NotImplementedException();
        }

        public override void SetJobParameter(string id, string name, string value)
        {
            throw new NotImplementedException();
        }

        public override void SetRangeInHash(string key, IEnumerable<KeyValuePair<string, string>> keyValuePairs)
        {
            throw new NotImplementedException();
        }

        #endregion

        public override IWriteOnlyTransaction CreateWriteTransaction()
        {
            return new MockWriteTransaction(this);
        }

        public override StateData GetStateData(string jobId)
        {
            return StateData;
        }

        public override HashSet<string> GetAllItemsFromSet(string key)
        {
            return new HashSet<string>(Sets.Where(x => x.Key == key).OrderBy(x => x.Id).Select(x => x.Value));
        }

        public override long GetSetCount(string key)
        {
            return Sets.Where(x => x.Key == key).Count();
        }

        public override List<string> GetRangeFromSet(string key, int startingFrom, int endingAt)
        {
            return Sets.Where(x => x.Key == key).OrderBy(x => x.Id).Where((x, i) => i >= startingFrom && i <= endingAt).Select(x => x.Value).ToList();
        }

        public override TimeSpan GetSetTtl(string key)
        {
            var expire = Sets.Where(x => x.Key == key && x.ExpireAt.HasValue).Min(x => x.ExpireAt);
            if (expire == null) return TimeSpan.FromSeconds(-1);

            return DateTime.UtcNow - expire.Value;
        }

        public override List<string> GetAllItemsFromList(string key)
        {
            return Lists.Where(x => x.Key == key).OrderByDescending(x => x.Id).Select(x => x.Value).ToList();
        }

        public override long GetListCount(string key)
        {
            return Lists.Where(x => x.Key == key).Count();
        }

        public override List<string> GetRangeFromList(string key, int startingFrom, int endingAt)
        {
            return Lists.Where(x => x.Key == key).OrderByDescending(x => x.Id).Where((x, i) => i >= startingFrom && i <= endingAt).Select(x => x.Value).ToList();
        }

        public override TimeSpan GetListTtl(string key)
        {
            var expire = Lists.Where(x => x.Key == key && x.ExpireAt.HasValue).Min(x => x.ExpireAt);
            if (expire == null) return TimeSpan.FromSeconds(-1);

            return DateTime.UtcNow - expire.Value;
        }
    }
}
