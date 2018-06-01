using Hangfire.Console.Serialization;
using System;
using System.Globalization;

namespace Hangfire.Console.Monitoring
{
    /// <summary>
    /// Progress bar line
    /// </summary>
    public class ProgressBarDto : LineDto
    {
        internal ProgressBarDto(ConsoleLine line, DateTime referenceTimestamp) : base(line, referenceTimestamp)
        {
            Id = int.Parse(line.Message, CultureInfo.InvariantCulture);
            Name = line.ProgressName;
            // ReSharper disable once PossibleInvalidOperationException
            Progress = line.ProgressValue.Value;
        }

        /// <inheritdoc />
        public override LineType Type => LineType.ProgressBar;

        /// <summary>
        /// Returns identifier for a progress bar
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// Returns optional name for a progress bar
        /// </summary>
        public string Name { get; }
        
        /// <summary>
        /// Returns progress value for a progress bar
        /// </summary>
        public double Progress { get; internal set; }
    }
}
