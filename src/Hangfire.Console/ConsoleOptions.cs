using System;

namespace Hangfire.Console
{
    /// <summary>
    /// Configuration options for console.
    /// </summary>
    public class ConsoleOptions
    {
        /// <summary>
        /// Gets or sets expiration time for console messages.
        /// </summary>
        public TimeSpan ExpireIn { get; set; } = TimeSpan.FromDays(1);

        /// <summary>
        /// Gets or sets if console messages should follow the same retention policy as the parent job.
        /// When set to <c>true</c>, <see cref="ExpireIn"/> parameter is ignored.
        /// </summary>
        public bool FollowJobRetentionPolicy { get; set; } = true;
        
        /// <summary>
        /// Gets or sets console poll interval (in ms).
        /// </summary>
        public int PollInterval { get; set; } = 1000;

        /// <summary>
        /// Gets or sets background color for console.
        /// </summary>
        public string BackgroundColor { get; set; } = "#0d3163";

        /// <summary>
        /// Gets or sets text color for console.
        /// </summary>
        public string TextColor { get; set; } = "#ffffff";

        /// <summary>
        /// Gets or sets timestamp color for console.
        /// </summary>
        public string TimestampColor { get; set; } = "#00aad7";


        internal void Validate(string paramName)
        {
            if (ExpireIn < TimeSpan.FromMinutes(1))
                throw new ArgumentException("ExpireIn shouldn't be less than 1 minute", paramName);

            if (PollInterval < 100)
                throw new ArgumentException("PollInterval shouldn't be less than 100 ms", paramName);
        }


    }
}
