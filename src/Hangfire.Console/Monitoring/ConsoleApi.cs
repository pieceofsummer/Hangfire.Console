using Hangfire.Console.Serialization;
using Hangfire.Console.Storage;
using Hangfire.Storage;
using System;
using System.Collections.Generic;

namespace Hangfire.Console.Monitoring
{
    internal class ConsoleApi : IConsoleApi
    {
        private readonly IConsoleStorage _storage;

        public ConsoleApi(IStorageConnection connection)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            _storage = new ConsoleStorage(connection);
        }

        public void Dispose()
        {
            _storage.Dispose();
        }

        public IList<LineDto> GetLines(string jobId, DateTime timestamp, LineType type = LineType.Any)
        {
            var consoleId = new ConsoleId(jobId, timestamp);

            var count = _storage.GetLineCount(consoleId);
            var result = new List<LineDto>(count);

            if (count > 0)
            {
                Dictionary<string, ProgressBarDto> progressBars = null;

                foreach (var entry in _storage.GetLines(consoleId, 0, count))
                {
                    if (entry.ProgressValue.HasValue)
                    {
                        if (type == LineType.Text) continue;

                        // aggregate progress value updates into single record

                        if (progressBars != null)
                        {
                            if (progressBars.TryGetValue(entry.Message, out var prev))
                            {
                                prev.Progress = entry.ProgressValue.Value;
                                prev.Color = entry.TextColor;
                                continue;
                            }
                        }
                        else
                        {
                            progressBars = new Dictionary<string, ProgressBarDto>();
                        }

                        var line = new ProgressBarDto(entry, timestamp);

                        progressBars.Add(entry.Message, line);
                        result.Add(line);
                    }
                    else
                    {
                        if (type == LineType.ProgressBar) continue;
                        
                        result.Add(new TextLineDto(entry, timestamp));
                    }
                }
            }

            return result;
        }
    }
}
