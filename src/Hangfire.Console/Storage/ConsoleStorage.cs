using System;
using System.Collections.Generic;
using System.Globalization;
using Hangfire.Console.Serialization;
using Hangfire.Storage;
using Hangfire.Common;

namespace Hangfire.Console.Storage
{
    internal class ConsoleStorage : IConsoleStorage
    {
        private const int ValueFieldLimit = 256;

        private readonly JobStorageConnection _connection;

        public ConsoleStorage(IStorageConnection connection)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            if (!(connection is JobStorageConnection jobStorageConnection))
                throw new NotSupportedException("Storage connections must implement JobStorageConnection");
            
            _connection = jobStorageConnection;
        }

        public void Dispose()
        {
            _connection.Dispose();
        }

        public void InitConsole(ConsoleId consoleId)
        {
            if (consoleId == null)
                throw new ArgumentNullException(nameof(consoleId));

            // We add an extra "jobId" record into Hash for console,
            // to correctly track TTL even if console contains no lines

            using (var transaction = _connection.CreateWriteTransaction())
            {
                if (!(transaction is JobStorageTransaction))
                    throw new NotSupportedException("Storage tranactions must implement JobStorageTransaction");
                
                transaction.SetRangeInHash(consoleId.GetHashKey(), new[] { new KeyValuePair<string, string>("jobId", consoleId.JobId) });
                
                transaction.Commit();
            }
        }

        public void AddLine(ConsoleId consoleId, ConsoleLine line)
        {
            if (consoleId == null)
                throw new ArgumentNullException(nameof(consoleId));
            if (line == null)
                throw new ArgumentNullException(nameof(line));
            if (line.IsReference)
                throw new ArgumentException("Cannot add reference directly", nameof(line));
            
            using (var tran = _connection.CreateWriteTransaction())
            {
                // check if encoded message fits into Set's Value field

                string value;

                if (line.Message.Length > ValueFieldLimit - 36)
                {
                    // pretty sure it won't fit
                    // (36 is an upper bound for JSON formatting, TimeOffset and TextColor)
                    value = null;
                }
                else
                {
                    // try to encode and see if it fits
                    value = JobHelper.ToJson(line);

                    if (value.Length > ValueFieldLimit)
                    {
                        value = null;
                    }
                }
                
                if (value == null)
                {
                    var referenceKey = Guid.NewGuid().ToString("N");

                    tran.SetRangeInHash(consoleId.GetHashKey(), new[] { new KeyValuePair<string, string>(referenceKey, line.Message) });

                    line.Message = referenceKey;
                    line.IsReference = true;

                    value = JobHelper.ToJson(line);
                }

                tran.AddToSet(consoleId.GetSetKey(), value, line.TimeOffset);

                if (line.ProgressValue.HasValue && line.Message == "1")
                {
                    var progress = line.ProgressValue.Value.ToString(CultureInfo.InvariantCulture);
                    
                    tran.SetRangeInHash(consoleId.GetHashKey(), new[] { new KeyValuePair<string, string>("progress", progress) });
                }
                
                tran.Commit();
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

            using (var tran = (JobStorageTransaction)_connection.CreateWriteTransaction())
            using (var expiration = new ConsoleExpirationTransaction(tran))
            {
                expiration.Expire(consoleId, expireIn);

                tran.Commit();
            }
        }

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

                yield return line;
            }
        }

        public StateData GetState(ConsoleId consoleId)
        {
            if (consoleId == null)
                throw new ArgumentNullException(nameof(consoleId));

            return _connection.GetStateData(consoleId.JobId);
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
    }
}
