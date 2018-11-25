using System;
using System.Collections.Generic;
using Hangfire.Common;
using Hangfire.Console.Utils;
using Hangfire.Storage;

namespace Hangfire.Console.Serialization
{
    /// <summary>
    /// Console identifier
    /// </summary>
    internal class ConsoleId : IEquatable<ConsoleId>
    {
        private string _cachedString;
        
        /// <summary>
        /// Initializes an instance of <see cref="ConsoleId"/>.
        /// </summary>
        /// <param name="jobId">Job identifier</param>
        /// <param name="timestamp">Date and time</param>
        public ConsoleId(string jobId, DateTime timestamp) : this(jobId, timestamp.ToUnixTime())
        {
        }
        
        /// <summary>
        /// Initializes an instance of <see cref="ConsoleId"/>.
        /// </summary>
        /// <param name="jobId">Job identifier</param>
        /// <param name="timestamp">Unix timestamp (in milliseconds)</param>
        public ConsoleId(string jobId, long timestamp)
        {
            if (string.IsNullOrEmpty(jobId))
                throw new ArgumentNullException(nameof(jobId));
            if (timestamp <= 0 || timestamp > int.MaxValue * 1000L)
                throw new ArgumentOutOfRangeException(nameof(timestamp));
            
            JobId = jobId;
            Timestamp = timestamp;
        }
        
        /// <summary>
        /// Job identifier
        /// </summary>
        public string JobId { get; }

        /// <summary>
        /// Timestamp
        /// </summary>
        public long Timestamp { get; }

        /// <summary>
        /// Returns <see cref="Timestamp"/> value as <see cref="DateTime"/>.
        /// </summary>
        public DateTime DateValue => UnixTime.ToDateTime(Timestamp);

        #region System.Object overrides

        /// <inheritdoc />
        public override string ToString()
        {
            if (_cachedString == null)
            {
                var buffer = new char[11 + JobId.Length];

                var timestamp = Timestamp;
                for (var i = 0; i < 11; i++, timestamp >>= 4)
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
        public override bool Equals(object obj) => Equals(obj as ConsoleId);

        /// <inheritdoc />
        public override int GetHashCode() => (JobId.GetHashCode() * 17) ^ Timestamp.GetHashCode();
        
        #endregion
        
        #region IEquatable<ConsoleId> implementation
        
        /// <inheritdoc />
        public bool Equals(ConsoleId other)
        {
            if (ReferenceEquals(other, null)) return false;
            if (ReferenceEquals(other, this)) return true;
            
            return other.Timestamp == Timestamp && 
                   string.Equals(other.JobId, JobId, StringComparison.OrdinalIgnoreCase);
        }

        #endregion

        /// <summary>
        /// Tests equality of two <see cref="ConsoleId"/> instances.
        /// </summary>
        public static bool operator ==(ConsoleId x, ConsoleId y) => x?.Equals(y) ?? ReferenceEquals(y, null);

        /// <summary>
        /// Tests inequality of two <see cref="ConsoleId"/> instances.
        /// </summary>
        public static bool operator !=(ConsoleId x, ConsoleId y) => !(x == y);
        
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
            for (var i = 10; i >= 0; i--)
            {
                var c = value[i] | 0x20;

                var x = (c >= '0' && c <= '9') ? (c - '0') : (c >= 'a' && c <= 'f') ? (c - 'a' + 10) : -1;
                if (x == -1)
                    throw new ArgumentException("Invalid value", nameof(value));

                timestamp = (timestamp << 4) + x;
            }

            return new ConsoleId(value.Substring(11), timestamp) { _cachedString = value };
        }
        
        /// <summary>
        /// Attempts to create an instance of <see cref="ConsoleId"/> from <see cref="StateData"/>.
        /// </summary>
        internal static bool TryCreate(string jobId, StateData state, out ConsoleId consoleId)
        {
            consoleId = null;
            return state.IsProcessingState() && TryCreate(jobId, state.Data, out consoleId);
        }

        /// <summary>
        /// Attempts to create an instance of <see cref="ConsoleId"/> from state dictionary.
        /// </summary>
        internal static bool TryCreate(string jobId, IDictionary<string, string> state, out ConsoleId consoleId)
        {
            if (state == null || !state.TryGetValue("StartedAt", out var startedAt))
            {
                // missing StartedAt field
                consoleId = null;
                return false;
            }
            
            consoleId = new ConsoleId(jobId, JobHelper.DeserializeDateTime(startedAt));
            return true;
        }
    }
}
