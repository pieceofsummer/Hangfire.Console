using System;

namespace Hangfire.Console.Serialization
{
    /// <summary>
    /// Console identifier
    /// </summary>
    internal class ConsoleId : IEquatable<ConsoleId>
    {
        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        
        private string _cachedString = null;

        /// <summary>
        /// Job identifier
        /// </summary>
        public string JobId { get; }

        /// <summary>
        /// Processing timestamp
        /// </summary>
        public DateTime Timestamp { get; }
        
        /// <summary>
        /// Initializes an instance of <see cref="ConsoleId"/>
        /// </summary>
        /// <param name="jobId">Job identifier</param>
        /// <param name="timestamp">Processing timestamp</param>
        public ConsoleId(string jobId, DateTime timestamp)
        {
            if (string.IsNullOrEmpty(jobId))
                throw new ArgumentNullException(nameof(jobId));

            JobId = jobId;
            Timestamp = timestamp.ToUniversalTime();
        }
        
        /// <summary>
        /// Creates an instance of <see cref="ConsoleId"/> from string representation.
        /// </summary>
        /// <param name="value">String</param>
        public static ConsoleId Parse(string value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            if (value.Length < 12)
                throw new ArgumentException("Invalid value", nameof(value));
            
            // Timestamp is serialized in reverse order for better randomness!

            long timestamp = 0;
            for (int i = 10; i >= 0; i--)
            {
                var c = value[i] | 0x20;

                var x = (c >= '0' && c <= '9') ? (c - '0') : (c >= 'a' && c <= 'f') ? (c - 'a' + 10) : -1;
                if (x == -1)
                    throw new ArgumentException("Invalid value", nameof(value));

                timestamp = (timestamp << 4) | (long)x;
            }

            return new ConsoleId(value.Substring(11), UnixEpoch.AddMilliseconds(timestamp)) { _cachedString = value };
        }

        /// <summary>
        /// Determines if this instance is equal to <paramref name="other"/> instance.
        /// </summary>
        /// <param name="other">Other instance</param>
        public bool Equals(ConsoleId other)
        {
            if (ReferenceEquals(other, null)) return false;
            if (ReferenceEquals(other, this)) return true;

            return other.Timestamp == Timestamp 
                && other.JobId == JobId;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            if (_cachedString == null)
            {
                var buffer = new char[11 + JobId.Length];

                var timestamp = (long)(Timestamp - UnixEpoch).TotalMilliseconds;
                for (int i = 0; i < 11; i++, timestamp >>= 4)
                {
                    var c = timestamp & 0x0F;
                    buffer[i] = (c < 10) ? (char)(c + '0') : (char)(c - 10 + 'a');
                }

                JobId.CopyTo(0, buffer, 11, JobId.Length);

                _cachedString = new string(buffer);
            }

            return _cachedString;
        }
        
        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return Equals(obj as ConsoleId);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return (JobId.GetHashCode() * 17) ^ Timestamp.GetHashCode();
        }
    }
}
