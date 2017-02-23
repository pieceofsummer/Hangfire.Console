using Hangfire.Console.Serialization;
using Hangfire.Console.Storage;
using Hangfire.Storage;
using Moq;
using System;
using Xunit;

namespace Hangfire.Console.Tests.Storage
{
    public class ConsoleExpirationTransactionFacts
    {
        private readonly ConsoleId _consoleId;
        
        private readonly Mock<JobStorageTransaction> _transaction;

        public ConsoleExpirationTransactionFacts()
        {
            _consoleId = new ConsoleId("1", DateTime.UtcNow);
            
            _transaction = new Mock<JobStorageTransaction>();
        }

        [Fact]
        public void Ctor_ThrowsException_IfTransactionIsNull()
        {
            Assert.Throws<ArgumentNullException>("transaction", () => new ConsoleExpirationTransaction(null));
        }

        [Fact]
        public void Dispose_ReallyDisposesTransaction()
        {
            var expiration = new ConsoleExpirationTransaction(_transaction.Object);

            expiration.Dispose();

            _transaction.Verify(x => x.Dispose());
        }
        
        [Fact]
        public void Expire_ThrowsException_IfConsoleIdIsNull()
        {
            var expiration = new ConsoleExpirationTransaction(_transaction.Object);

            Assert.Throws<ArgumentNullException>("consoleId", () => expiration.Expire(null, TimeSpan.FromHours(1)));
        }
        
        [Fact]
        public void Expire_ExpiresSetAndHash()
        {
            var expiration = new ConsoleExpirationTransaction(_transaction.Object);

            expiration.Expire(_consoleId, TimeSpan.FromHours(1));

            _transaction.Verify(x => x.ExpireSet(_consoleId.GetSetKey(), It.IsAny<TimeSpan>()));
            _transaction.Verify(x => x.ExpireHash(_consoleId.GetHashKey(), It.IsAny<TimeSpan>()));

            // backward compatibility:
            _transaction.Verify(x => x.ExpireSet(_consoleId.GetOldConsoleKey(), It.IsAny<TimeSpan>()));
            _transaction.Verify(x => x.ExpireHash(_consoleId.GetOldConsoleKey(), It.IsAny<TimeSpan>()));
        }

        [Fact]
        public void Persist_ThrowsException_IfConsoleIdIsNull()
        {
            var expiration = new ConsoleExpirationTransaction(_transaction.Object);

            Assert.Throws<ArgumentNullException>("consoleId", () => expiration.Persist(null));
        }
        
        [Fact]
        public void Persist_PersistsSetAndHash()
        {
            var expiration = new ConsoleExpirationTransaction(_transaction.Object);

            expiration.Persist(_consoleId);

            _transaction.Verify(x => x.PersistSet(_consoleId.GetSetKey()));
            _transaction.Verify(x => x.PersistHash(_consoleId.GetHashKey()));

            // backward compatibility:
            _transaction.Verify(x => x.PersistSet(_consoleId.GetOldConsoleKey()));
            _transaction.Verify(x => x.PersistHash(_consoleId.GetOldConsoleKey()));
        }

    }
}
