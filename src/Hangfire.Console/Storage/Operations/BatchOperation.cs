using System;
using System.Collections.Generic;
using System.Linq;
using Hangfire.Console.Serialization;
using Hangfire.Storage;

namespace Hangfire.Console.Storage.Operations
{
    internal class BatchOperation : Operation
    {
        private readonly List<LineOperation> _lines = new List<LineOperation>();
        private IDictionary<string, ProgressBarOperation> _progressBars;
        private InitOperation _init;
        private PersistOperation _persist;

        public BatchOperation(ConsoleId consoleId) : base(consoleId)
        {
        }

        /// <summary>
        /// Returns number of operations in the batch.
        /// </summary>
        public override int Count => (_init != null ? 1 : 0) + _lines.Count + (_persist != null ? 1 : 0);
        
        /// <summary>
        /// Adds a line/progressbar operation to the batch.
        /// </summary>
        private void AddLine(LineOperation operation)
        {
            if (operation is ProgressBarOperation progress)
            {
                if (_progressBars == null)
                {
                    _progressBars = new Dictionary<string, ProgressBarOperation>(StringComparer.Ordinal);
                }
                else if (_progressBars.TryGetValue(progress.ProgressBarId, out var prev))
                {
                    if (progress.TimeOffset > prev.TimeOffset)
                    {
                        // update existing operation with the most recent value
                        prev.Value = progress.Value;
                    }
                    else
                    {
                        // Previous value is already the most recent one, no update needed.
                        // Instead, the name and color should be copied back, because
                        // current operation could be the initial progressbar line.
                        prev.Name = progress.Name;
                        prev.Color = progress.Color;
                    }
                    return;
                }

                _progressBars.Add(progress.ProgressBarId, progress);
            }
            
            _lines.Add(operation);
        }

        /// <summary>
        /// Updates the initialization operation for this batch.
        /// </summary>
        private void SetInit(InitOperation operation)
        {
            if (operation == null) return;

            _init = operation;
        }

        /// <summary>
        /// Updates the persist/expire operation for this batch.
        /// </summary>
        private void SetPersist(PersistOperation operation)
        {
            if (operation == null) return;

            if (_persist == null || _persist.Timestamp < operation.Timestamp)
            {
                // update with the most recent persist operation
                _persist = operation;
            }
        }

        /// <summary>
        /// Adds <paramref name="operation"/> to the batch.
        /// </summary>
        /// <param name="operation">Operation</param>
        private int EnqueueUnsafe(Operation operation)
        {
            var prevCount = Count;
            
            switch (operation)
            {
                case BatchOperation batch:
                    // The entire batch might be re-queued (e.g. if previous commit was unsuccessful)
                    
                    if (_progressBars == null)
                    {
                        // Fast path: no progressbar updates added yet.
                        // Just copy lines and p
                        
                        if (batch._progressBars != null)
                            _progressBars = new Dictionary<string, ProgressBarOperation>(batch._progressBars);
                        
                        _lines.AddRange(batch._lines);
                    }
                    else
                    {
                        // Slow path: already has progressBars.
                        // For progress updates to be merged correctly, add lines one by one.
                        
                        foreach (var line in batch._lines)
                            AddLine(line);
                    }

                    SetInit(batch._init);
                    SetPersist(batch._persist);
                    break;
                
                case LineOperation line:
                    AddLine(line);
                    break;
                
                case InitOperation init:
                    SetInit(init);
                    break;
                
                case PersistOperation persist:
                    SetPersist(persist);
                    break;
                
                default:
                    throw new ArgumentException("Unknown operation", nameof(operation));
            }

            return Count - prevCount;
        }

        /// <summary>
        /// Adds <paramref name="operation"/> to the batch.
        /// </summary>
        /// <param name="operation">Operation</param>
        /// <exception cref="ArgumentNullException">Operation is null</exception>
        /// <exception cref="ArgumentException">Console id of the operation doesn't match that of the batch</exception>
        public int Enqueue(Operation operation)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));
            if (operation.ConsoleId != ConsoleId)
                throw new ArgumentException("ConsoleId of the operation doesn't match ours", nameof(operation));
            
            lock (this)
            {
                return EnqueueUnsafe(operation);
            }
        }

        /// <inheritdoc />
        public override void Apply(JobStorageTransaction transaction)
        {
            if (transaction == null)
                throw new ArgumentNullException(nameof(transaction));

            _init?.Apply(transaction);
            
            foreach (var line in _lines.OrderBy(x => x.TimeOffset))
            {
                line.Apply(transaction);
            }

            _persist?.Apply(transaction);
        }
    }
}