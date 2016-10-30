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

                    tran.SetRangeInHash(consoleId.ToString(), new[] { new KeyValuePair<string, string>(referenceKey, line.Message) });

                    line.Message = referenceKey;
                    line.IsReference = true;

                    value = JobHelper.ToJson(line);
                }

                tran.AddToSet(consoleId.ToString(), value);

                tran.Commit();
            }
        }
        
        public void Expire(ConsoleId consoleId, TimeSpan expireIn)
        {
            if (consoleId == null)
                throw new ArgumentNullException(nameof(consoleId));

            using (var tran = (JobStorageTransaction)_connection.CreateWriteTransaction())
            {
                tran.ExpireSet(consoleId.ToString(), expireIn);
                tran.ExpireHash(consoleId.ToString(), expireIn);
                tran.Commit();
            }
        }

        public int GetLineCount(ConsoleId consoleId)
        {
            if (consoleId == null)
                throw new ArgumentNullException(nameof(consoleId));

            return (int)_connection.GetSetCount(consoleId.ToString());
        }

        public IEnumerable<ConsoleLine> GetLines(ConsoleId consoleId, int start, int end)
        {
            if (consoleId == null)
                throw new ArgumentNullException(nameof(consoleId));

            foreach (var item in _connection.GetRangeFromSet(consoleId.ToString(), start, end))
            {
                var line = JobHelper.FromJson<ConsoleLine>(item);

                if (line.IsReference)
                {
                    line.Message = _connection.GetValueFromHash(consoleId.ToString(), line.Message);
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
    }
}
