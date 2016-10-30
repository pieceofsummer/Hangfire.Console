using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Hangfire.Console.Tests.Mocks
{
    public class MockCancellationToken : IJobCancellationToken
    {
        private readonly CancellationToken _shutdownToken;

        public MockCancellationToken(CancellationToken cancellationToken = default(CancellationToken))
        {
            _shutdownToken = cancellationToken;
        }

        public CancellationToken ShutdownToken => _shutdownToken;

        public void ThrowIfCancellationRequested()
        {
            _shutdownToken.ThrowIfCancellationRequested();
        }
    }
}
