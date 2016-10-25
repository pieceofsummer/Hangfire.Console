using Hangfire.Console.Serialization;
using System;
using Xunit;

namespace Hangfire.Console.Tests.Serialization
{
    public class ConsoleIdFacts
    {
        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        [Fact]
        public void Ctor_ThrowsAnException_WhenJobIdIsNull()
        {
            Assert.Throws<ArgumentNullException>("jobId", () => new ConsoleId(null, DateTime.UtcNow));
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenJobIdIsEmpty()
        {
            Assert.Throws<ArgumentNullException>("jobId", () => new ConsoleId("", DateTime.UtcNow));
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenTimestampBeforeEpoch()
        {
            Assert.Throws<ArgumentOutOfRangeException>("timestamp", 
                () => new ConsoleId("123", UnixEpoch));
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenTimestampAfterEpochPlusMaxInt()
        {
            Assert.Throws<ArgumentOutOfRangeException>("timestamp", 
                () => new ConsoleId("123", UnixEpoch.AddSeconds(int.MaxValue).AddSeconds(1)));
        }
        
        [Fact]
        public void SerializesCorrectly()
        {
            var x = new ConsoleId("123", new DateTime(2016, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            var s = x.ToString();
            Assert.Equal("00cdb7af151123", s);
        }

        [Fact]
        public void IgnoresFractionalMilliseconds()
        {
            var x = new ConsoleId("123", UnixEpoch.AddMilliseconds(3.0));
            var y = new ConsoleId("123", UnixEpoch.AddMilliseconds(3.0215));
            Assert.Equal(x, y);
        }

        [Fact]
        public void Parse_ThrowsAnException_WhenValueIsNull()
        {
            Assert.Throws<ArgumentNullException>("value", () => ConsoleId.Parse(null));
        }

        [Fact]
        public void Parse_ThrowsAnException_WhenValueIsTooShort()
        {
            Assert.Throws<ArgumentException>("value", () => ConsoleId.Parse("00cdb7af1"));
        }

        [Fact]
        public void Parse_ThrowsAnException_WhenValueIsInvalid()
        {
            Assert.Throws<ArgumentException>("value", () => ConsoleId.Parse("00x00y00z001"));
        }

        [Fact]
        public void DeserializesCorrectly()
        {
            var x = ConsoleId.Parse("00cdb7af151123");
            Assert.Equal("123", x.JobId);
            Assert.Equal(new DateTime(2016, 1, 1, 0, 0, 0, DateTimeKind.Utc), x.DateValue);
        }
    }
}
