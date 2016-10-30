using Hangfire.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using Hangfire.States;

namespace Hangfire.Console.Tests.Mocks
{
    public class MockWriteTransaction : JobStorageTransaction
    {
        private readonly MockStorageConnection _connection;

        public MockWriteTransaction(MockStorageConnection connection)
        {
            _connection = connection;
        }

        #region NotImplemented

        public override void AddJobState(string jobId, IState state)
        {
            throw new NotImplementedException();
        }

        public override void AddToQueue(string queue, string jobId)
        {
            throw new NotImplementedException();
        }
        
        public override void DecrementCounter(string key)
        {
            throw new NotImplementedException();
        }

        public override void DecrementCounter(string key, TimeSpan expireIn)
        {
            throw new NotImplementedException();
        }

        public override void ExpireJob(string jobId, TimeSpan expireIn)
        {
            throw new NotImplementedException();
        }

        public override void IncrementCounter(string key)
        {
            throw new NotImplementedException();
        }

        public override void IncrementCounter(string key, TimeSpan expireIn)
        {
            throw new NotImplementedException();
        }

        public override void PersistJob(string jobId)
        {
            throw new NotImplementedException();
        }
        
        public override void RemoveHash(string key)
        {
            throw new NotImplementedException();
        }

        public override void SetJobState(string jobId, IState state)
        {
            throw new NotImplementedException();
        }

        public override void SetRangeInHash(string key, IEnumerable<KeyValuePair<string, string>> keyValuePairs)
        {
            throw new NotImplementedException();
        }
        
        #endregion

        public override void Commit()
        {
        }
            
        public override void AddToSet(string key, string value)
        {
            AddToSet(key, value, 0);
        }

        public override void AddToSet(string key, string value, double score)
        {
            var existing = _connection.Sets.FirstOrDefault(x => x.Key == key && x.Value == value);
            if (existing == null)
            {
                existing = new MockSetEntry() { Key = key, Value = value };
                _connection.Sets.Add(existing);
            }

            existing.Score = score;
        }
        
        public override void RemoveFromSet(string key, string value)
        {
            _connection.Sets.RemoveAll(x => x.Key == key && x.Value == value);
        }

        public override void ExpireSet(string key, TimeSpan expireIn)
        {
            foreach (var x in _connection.Sets.Where(x => x.Key == key))
            {
                x.ExpireAt = DateTime.UtcNow + expireIn;
            }
        }

        public override void PersistSet(string key)
        {
            foreach (var x in _connection.Sets.Where(x => x.Key == key))
            {
                x.ExpireAt = null;
            }
        }

        public override void AddRangeToSet(string key, IList<string> items)
        {
            foreach (var x in items)
            {
                AddToSet(key, x);
            }
        }

        public override void RemoveSet(string key)
        {
            _connection.Sets.RemoveAll(x => x.Key == key);
        }

        public override void InsertToList(string key, string value)
        {
            _connection.Lists.Add(new MockSetEntry() { Key = key, Value = value });
        }

        public override void RemoveFromList(string key, string value)
        {
            _connection.Lists.RemoveAll(x => x.Key == key && x.Value == value);
        }

        public override void ExpireList(string key, TimeSpan expireIn)
        {
            foreach (var x in _connection.Lists.Where(x => x.Key == key))
            {
                x.ExpireAt = DateTime.UtcNow + expireIn;
            }
        }

        public override void PersistList(string key)
        {
            foreach (var x in _connection.Lists.Where(x => x.Key == key))
            {
                x.ExpireAt = null;
            }
        }
        
        public override void TrimList(string key, int keepStartingFrom, int keepEndingAt)
        {
            var keep = _connection.Lists.Where(x => x.Key == key).OrderByDescending(x => x.Id).Where((x, i) => i >= keepStartingFrom && i <= keepEndingAt).ToList();
            _connection.Lists.Clear();
            _connection.Lists.AddRange(keep);
        }

    }
}
