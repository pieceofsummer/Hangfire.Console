using System;

namespace Hangfire.Console.Utils
{
    /// <summary>
    /// Provides helper methods to convert <see cref="DateTime"/> to/from Unix timestamps
    /// </summary>
    internal static class UnixTime
    {
        internal static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static DateTime ToDateTime(long timestamp) => Epoch.AddMilliseconds(timestamp);
        
        /// <summary>
        /// Returns <paramref name="date"/> as a number of milliseconds since Jan 1 1970.
        /// </summary>
        /// <param name="date">Date and time</param>
        /// <returns>Unix timestamp</returns>
        public static long ToUnixTime(this DateTime date) => (long)(date - Epoch).TotalMilliseconds;
    }
}