using System;

namespace Hangfire.Console
{
    /// <summary>
    /// Configuration options for console.
    /// </summary>
    public class ConsoleOptions
    {
        /// <summary>
        /// Current options
        /// </summary>
        internal static ConsoleOptions Current { get; set; }

        /// <summary>
        /// Gets or sets expiration time for console messages.
        /// </summary>
        public TimeSpan ExpireIn { get; set; } = TimeSpan.FromDays(1);
    }
}
