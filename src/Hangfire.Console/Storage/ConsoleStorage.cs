using System;
using System.Collections.Generic;
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

            _connection = (JobStorageConnection)connection;
        }
        
        public void Dispose()
        {
            _connection.Dispose();
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

                    tran.SetRangeInHash(GetHashKey(consoleId), new[] { new KeyValuePair<string, string>(referenceKey, line.Message) });

                    line.Message = referenceKey;
                    line.IsReference = true;

                    value = JobHelper.ToJson(line);
                }

                tran.AddToSet(GetSetKey(consoleId), value, line.TimeOffset);

                tran.Commit();
            }
        }
        
        public void Expire(ConsoleId consoleId, TimeSpan expireIn)
        {
            if (consoleId == null)
                throw new ArgumentNullException(nameof(consoleId));

            using (var tran = (JobStorageTransaction)_connection.CreateWriteTransaction())
            {
                tran.ExpireSet(GetSetKey(consoleId), expireIn);
                tran.ExpireHash(GetHashKey(consoleId), expireIn);

                // After upgrading to Hangfire.Console version with new keys, 
                // there may be existing background jobs with console attached 
                // to the previous keys. We should expire them also.
                tran.ExpireSet(GetOldConsoleKey(consoleId), expireIn);
                tran.ExpireHash(GetOldConsoleKey(consoleId), expireIn);

                tran.Commit();
            }
        }

        public int GetLineCount(ConsoleId consoleId)
        {
            if (consoleId == null)
                throw new ArgumentNullException(nameof(consoleId));

            var result = (int)_connection.GetSetCount(GetSetKey(consoleId));

            if (result == 0)
            {
                // Read operations should be backwards compatible and use
                // old keys, if new one don't contain any data.
                return (int)_connection.GetSetCount(GetOldConsoleKey(consoleId));
            }

            return result;
        }

        public IEnumerable<ConsoleLine> GetLines(ConsoleId consoleId, int start, int end)
        {
            if (consoleId == null)
                throw new ArgumentNullException(nameof(consoleId));

            var useOldKeys = false;
            var items = _connection.GetRangeFromSet(GetSetKey(consoleId), start, end);

            if (items == null || items.Count == 0)
            {
                // Read operations should be backwards compatible and use
                // old keys, if new one don't contain any data.
                items = _connection.GetRangeFromSet(GetOldConsoleKey(consoleId), start, end);
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
                            line.Message = _connection.GetValueFromHash(GetOldConsoleKey(consoleId), line.Message);
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
                        line.Message = _connection.GetValueFromHash(GetHashKey(consoleId), line.Message);
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

        private string GetSetKey(ConsoleId consoleId)
        {
            return $"console:{consoleId}";
        }

        private string GetHashKey(ConsoleId consoleId)
        {
            return $"console:refs:{consoleId}";
        }

        private string GetOldConsoleKey(ConsoleId consoleId)
        {
            return consoleId.ToString();
        }
    }
}
