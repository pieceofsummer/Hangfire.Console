using System;
using System.Collections.Generic;
using System.Globalization;
using Hangfire.Console.Serialization;
using Hangfire.Storage;
using Hangfire.Common;
using Hangfire.Console.Storage.Operations;

namespace Hangfire.Console.Storage
{
    internal class ConsoleStorage : IConsoleStorageRead, IConsoleStorageWrite
    {
        private readonly JobStorageConnection _connection;
        private readonly IOperationStream _stream;
        
        public ConsoleStorage(IStorageConnection connection, IOperationStream stream = null)
        {
            _connection = (JobStorageConnection)connection ?? throw new ArgumentNullException(nameof(connection));
            _stream = stream; // can be null
        }

        public void Dispose()
        {
            _connection.Dispose();
        }
        
        private void Write(Operation operation, bool sync = false)
        {
            if (_stream == null || sync)
            {
                // commit synchronously
                
                using (var transaction = (JobStorageTransaction)_connection.CreateWriteTransaction())
                {
                    operation.Apply(transaction);
                    transaction.Commit();
                }
            }
            else
            {
                // post to Central stream
                
                _stream.Write(operation);
            }
        }
        
        #region IConsoleStorageWrite
        
        public void InitConsole(ConsoleId consoleId)
        {
            if (consoleId == null)
                throw new ArgumentNullException(nameof(consoleId));

            // We add an extra "jobId" record into Hash for console,
            // to correctly track TTL even if console contains no lines
            Write(new InitOperation(consoleId), true);
        }

        public void AddLine(ConsoleId consoleId, ConsoleLine line)
        {
            if (consoleId == null)
                throw new ArgumentNullException(nameof(consoleId));
            if (line == null)
                throw new ArgumentNullException(nameof(line));
            if (line.IsReference)
                throw new ArgumentException("Cannot add reference directly", nameof(line));
            
            if (line.IsProgressBar)
            {
                Write(new ProgressBarOperation(consoleId, line));
            }
            else
            {
                Write(new LineOperation(consoleId, line));
            }
        }
        
        public TimeSpan GetConsoleTtl(ConsoleId consoleId)
        {
            if (consoleId == null)
                throw new ArgumentNullException(nameof(consoleId));

            return _connection.GetHashTtl(consoleId.GetHashKey());
        }

        public void Expire(ConsoleId consoleId, TimeSpan expireIn)
        {
            if (consoleId == null)
                throw new ArgumentNullException(nameof(consoleId));

            Write(new ExpireOperation(consoleId, expireIn));
        }
        
        #endregion

        #region IConsoleStorageRead
        
        public int GetLineCount(ConsoleId consoleId)
        {
            if (consoleId == null)
                throw new ArgumentNullException(nameof(consoleId));

            var result = (int)_connection.GetSetCount(consoleId.GetSetKey());

            if (result == 0)
            {
                // Read operations should be backwards compatible and use
                // old keys, if new one don't contain any data.
                return (int)_connection.GetSetCount(consoleId.GetOldConsoleKey());
            }

            return result;
        }

        public IEnumerable<ConsoleLine> GetLines(ConsoleId consoleId, int start, int end)
        {
            if (consoleId == null)
                throw new ArgumentNullException(nameof(consoleId));

            var useOldKeys = false;
            var items = _connection.GetRangeFromSet(consoleId.GetSetKey(), start, end);

            if (items == null || items.Count == 0)
            {
                // Read operations should be backwards compatible and use
                // old keys, if new one don't contain any data.
                items = _connection.GetRangeFromSet(consoleId.GetOldConsoleKey(), start, end);
                useOldKeys = true;
            }

            foreach (var item in items)
            {
                var line = JobHelper.FromJson<ConsoleLine>(item);

                if (line.IsReference)
                {
                    if (useOldKeys)
                    {
                        try
                        {
                            line.Message = _connection.GetValueFromHash(consoleId.GetOldConsoleKey(), line.Message);
                        }
                        catch
                        {
                            // This may happen, when using Hangfire.Redis storage and having
                            // background job, whose console session was stored using old key
                            // format.
                        }
                    }
                    else
                    {
                        line.Message = _connection.GetValueFromHash(consoleId.GetHashKey(), line.Message);
                    }
                    
                    line.IsReference = false;
                }
                else if (line.ProgressValue.HasValue)
                {
                    line.InitialValue = line.ProgressValue.Value;
                }

                yield return line;
            }
        }

        public StateData GetState(string jobId)
        {
            if (jobId == null)
                throw new ArgumentNullException(nameof(jobId));

            return _connection.GetStateData(jobId);
        }

        public double? GetProgress(ConsoleId consoleId)
        {
            if (consoleId == null)
                throw new ArgumentNullException(nameof(consoleId));

            var progress = _connection.GetValueFromHash(consoleId.GetHashKey(), "progress");
            if (string.IsNullOrEmpty(progress))
            {
                // progress value is not set
                return null;
            }

            try
            {
                return double.Parse(progress, CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                // corrupted data?
                return null;
            }
        }

        #endregion
    }
}
