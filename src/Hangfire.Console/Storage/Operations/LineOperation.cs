using System;
using System.Collections.Generic;
using Hangfire.Common;
using Hangfire.Console.Serialization;
using Hangfire.Storage;

namespace Hangfire.Console.Storage.Operations
{
    /// <summary>
    /// Line operation
    /// </summary>
    internal class LineOperation : Operation
    {
        private const int ValueFieldLimit = 256;

        /// <inheritdoc />
        public LineOperation(ConsoleId consoleId, ConsoleLine line) : base(consoleId)
        {
            Line = line ?? throw new ArgumentNullException(nameof(line));
            if (line.IsReference)
                throw new ArgumentException("Line cannot be reference", nameof(line));
        }
        
        /// <summary>
        /// Gets the underlying <see cref="ConsoleLine"/> object.
        /// </summary>
        public ConsoleLine Line { get; }

        /// <summary>
        /// Gets <see cref="ConsoleLine.TimeOffset"/> on the <see cref="Line"/>.
        /// </summary>
        public double TimeOffset
        {
            get => Line.TimeOffset;
            set => Line.TimeOffset = value;
        }
        
        /// <inheritdoc />
        public override string ToString() => $"'{Line.Message}'";
        
        /// <inheritdoc />
        public override void Apply(JobStorageTransaction transaction)
        {
            if (transaction == null)
                throw new ArgumentNullException(nameof(transaction));
            
            string serialized = null;
            
            // Estimate if the line could be possibly stored inline in Set table
            // (36 is an upper bound for JSON formatting, TimeOffset and TextColor)
            if (Line.Message.Length <= ValueFieldLimit - 36)
            {
                // try to encode and see if it actually fits
                serialized = JobHelper.ToJson(Line);

                if (serialized.Length > ValueFieldLimit)
                {
                    serialized = null;
                }
            }

            if (serialized == null)
            {
                // The line is long enough to exceed Set value field limit.
                // As a workaround, the original message would be stored in Hash table,
                // and the line entry in Set would rather contain a reference to it.
                var referenceKey = Guid.NewGuid().ToString("N");
                
                var reference = new ConsoleLine()
                {
                    TimeOffset = Line.TimeOffset,
                    IsReference = true,
                    Message = referenceKey,
                    TextColor = Line.TextColor,
                    ProgressValue = Line.ProgressValue,
                    ProgressName = Line.ProgressName
                };
                
                serialized = JobHelper.ToJson(reference);
                if (serialized.Length > ValueFieldLimit)
                    throw new InvalidOperationException($"Serialized reference line is still too long: {serialized}");
                
                transaction.SetRangeInHash(ConsoleId.GetHashKey(), new[] { new KeyValuePair<string, string>(referenceKey, Line.Message) });
            }

            transaction.AddToSet(ConsoleId.GetSetKey(), serialized, Line.TimeOffset);
        }
    }
}