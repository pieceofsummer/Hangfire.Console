using System;
using System.Collections.Generic;
using System.Globalization;
using Hangfire.Console.Serialization;
using Hangfire.Storage;

namespace Hangfire.Console.Storage.Operations
{
    /// <summary>
    /// ProgressBar operation
    /// </summary>
    internal class ProgressBarOperation : LineOperation
    {
        /// <inheritdoc />
        public ProgressBarOperation(ConsoleId consoleId, ConsoleLine line) : base(consoleId, line)
        {
            if (!line.IsProgressBar)
                throw new ArgumentException("Not a progressbar line", nameof(line));
        }

        /// <summary>
        /// Gets the progressbar id.
        /// </summary>
        public string ProgressBarId => Line.Message;

        /// <summary>
        /// Gets or sets <see cref="ConsoleLine.ProgressValue"/> on the <see cref="LineOperation.Line"/>.
        /// </summary>
        public double Value
        {
            // ReSharper disable once PossibleInvalidOperationException
            get => Line.ProgressValue.Value;
            set => Line.ProgressValue = value;
        }

        /// <summary>
        /// Gets or sets <see cref="ConsoleLine.ProgressName"/> on the <see cref="LineOperation.Line"/>.
        /// </summary>
        public string Name
        {
            get => Line.ProgressName;
            set => Line.ProgressName = value;
        }
        
        /// <summary>
        /// Gets or sets <see cref="ConsoleLine.TextColor"/> on the <see cref="LineOperation.Line"/>.
        /// </summary>
        public string Color
        {
            get => Line.TextColor;
            set => Line.TextColor = value;
        }

        /// <inheritdoc />
        public override OperationKey CreateKey() => new OperationKey(ConsoleId, ProgressBarId);
        
        /// <inheritdoc />
        public override string ToString() => $"{Value:F1}%";
        
        /// <inheritdoc />
        public override void Apply(JobStorageTransaction transaction)
        {
            base.Apply(transaction);
            
            if (ProgressBarId == "1")
            {
                var progress = string.Format(CultureInfo.InvariantCulture, "{0:0.#}", Value);
                
                transaction.SetRangeInHash(ConsoleId.GetHashKey(), new[] { new KeyValuePair<string, string>("progress", progress) });
            }
        }
    }

}