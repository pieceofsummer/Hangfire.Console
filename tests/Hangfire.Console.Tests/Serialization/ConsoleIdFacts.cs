using Hangfire.Console.Serialization;
using System;
using Xunit;

namespace Hangfire.Console.Tests.Serialization
{
    public class ConsoleIdFacts
    {
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
        public void SerializesCorrectly()
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
        public void DeserializesCorrectly()
        {
            var x = ConsoleId.Parse("00cdb7af151123");
            Assert.Equal("123", x.JobId);
            Assert.Equal(new DateTime(2016, 1, 1, 0, 0, 0, DateTimeKind.Utc), x.Timestamp);
        }
    }
}
