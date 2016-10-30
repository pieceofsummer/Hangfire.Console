using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Hangfire.Console.Tests.Mocks
{
    public class MockSetEntry
    {
        private static int _id = 1;

        public MockSetEntry()
        {
            Id = Interlocked.Increment(ref _id);
        }

        public int Id { get; }

        public string Key { get; set; }
        public double Score { get; set; }
        public string Value { get; set; }
        public DateTime? ExpireAt { get; set; }
    }
}
