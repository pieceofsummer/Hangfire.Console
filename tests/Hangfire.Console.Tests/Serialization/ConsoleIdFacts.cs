using Hangfire.Console.Serialization;
using System;
using Hangfire.Console.Utils;
using Xunit;

namespace Hangfire.Console.Tests.Serialization
{
    public class ConsoleIdFacts
    {
        [Fact]
        public void Ctor_ThrowsAnException_WhenJobIdIsNull()
        {
            Assert.Throws<ArgumentNullException>("jobId", () => new ConsoleId(null, DateTime.UtcNow));
            Assert.Throws<ArgumentNullException>("jobId", () => new ConsoleId(null, 0));
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenJobIdIsEmpty()
        {
            Assert.Throws<ArgumentNullException>("jobId", () => new ConsoleId("", DateTime.UtcNow));
            Assert.Throws<ArgumentNullException>("jobId", () => new ConsoleId("", 0));
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenTimestampBeforeEpoch()
        {
            Assert.Throws<ArgumentOutOfRangeException>("timestamp", 
                () => new ConsoleId("123", UnixTime.ToDateTime(0)));
            Assert.Throws<ArgumentOutOfRangeException>("timestamp", 
                () => new ConsoleId("123", 0));
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenTimestampAfterEpochPlusMaxInt()
        {
            Assert.Throws<ArgumentOutOfRangeException>("timestamp", 
                () => new ConsoleId("123", UnixTime.ToDateTime(int.MaxValue * 1000L).AddSeconds(1)));
            Assert.Throws<ArgumentOutOfRangeException>("timestamp", 
                () => new ConsoleId("123", int.MaxValue * 1000L + 1));
        }
        
        [Fact]
        public void Ctor_IgnoresFractionalMillisecondsInDateTime()
        {
            var x = new ConsoleId("123", UnixTime.Epoch.AddMilliseconds(3.0));
            var y = new ConsoleId("123", UnixTime.Epoch.AddMilliseconds(3.0215));
            Assert.Equal(x, y);
        }
        
        [Fact]
        public void ToString_SerializesCorrectly()
        {
            var x = new ConsoleId("123", new DateTime(2016, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            var s = x.ToString();
            Assert.Equal("00cdb7af151123", s);
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
        public void Parse_DeserializesCorrectly()
        {
            var x = ConsoleId.Parse("00cdb7af151123");
            Assert.Equal("123", x.JobId);
            Assert.Equal(new DateTime(2016, 1, 1, 0, 0, 0, DateTimeKind.Utc), x.DateValue);
        }
    }
}
