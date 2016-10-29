using Hangfire.Common;
using Hangfire.Console.Serialization;
using Hangfire.Dashboard;
using Hangfire.States;
using Hangfire.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace Hangfire.Console.Dashboard
{
    /// <summary>
    /// Helper methods to render console shared between
    /// <see cref="ProcessingStateRenderer"/> and <see cref="ConsoleDispatcher"/>.
    /// </summary>
    internal static class ConsoleRenderer
    {
        private static readonly HtmlHelper Helper = new HtmlHelper(new DummyPage());

        private class DummyPage : RazorPage
        {
            public override void Execute()
            {
            }
        }

        /// <summary>
        /// Renders a single <see cref="ConsoleLine"/> to a buffer.
        /// </summary>
        /// <param name="builder">Buffer</param>
        /// <param name="line">Line</param>
        /// <param name="timestamp">Reference timestamp for time offset</param>
        public static void RenderLine(StringBuilder builder, ConsoleLine line, DateTime timestamp)
        {
            var offset = TimeSpan.FromSeconds(line.TimeOffset);
            var isProgressBar = line.ProgressValue.HasValue;

            builder.Append("<div class=\"line").Append(isProgressBar ? " pb" : "").Append("\"");

            if (line.TextColor != null)
            {
                builder.Append(" style=\"color:").Append(line.TextColor).Append("\"");
            }

            if (isProgressBar)
            {
                builder.Append(" data-id=\"").Append(line.Message).Append("\"");
            }

            builder.Append(">")
                   .Append(Helper.MomentTitle(timestamp + offset, Helper.ToHumanDuration(offset)));

            if (isProgressBar)
            {
                builder.AppendFormat("<div class=\"pv\" style=\"width:{0}%\" data-value=\"{0}\"></div>", line.ProgressValue.Value);
            }
            else
            {
                builder.Append(Helper.HtmlEncode(line.Message));
            }

            builder.Append("</div>");
        }

        /// <summary>
        /// Renders a collection of <seealso cref="ConsoleLine"/> to a buffer.
        /// </summary>
        /// <param name="builder">Buffer</param>
        /// <param name="lines">Lines</param>
        /// <param name="timestamp">Reference timestamp for time offset</param>
        public static void RenderLines(StringBuilder builder, IEnumerable<ConsoleLine> lines, DateTime timestamp)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            if (lines == null) return;

            foreach (var line in lines)
            {
                RenderLine(builder, line, timestamp);
            }
        }
        
        /// <summary>
        /// Fetches and renders console line buffer.
        /// </summary>
        /// <param name="builder">Buffer</param>
        /// <param name="storage">Job storage</param>
        /// <param name="consoleId">Console identifier</param>
        /// <param name="start">Offset to read lines from</param>
        public static void RenderLineBuffer(StringBuilder builder, JobStorage storage, ConsoleId consoleId, int start)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));
            if (storage == null)
                throw new ArgumentNullException(nameof(storage));
            if (consoleId == null)
                throw new ArgumentNullException(nameof(consoleId));

            var items = ReadLines(storage, consoleId, ref start);

            builder.AppendFormat("<div class=\"line-buffer\" data-n=\"{1}\">", consoleId, start);
            RenderLines(builder, items, consoleId.DateValue);
            builder.Append("</div>");
        }

        /// <summary>
        /// Fetches console lines from storage.
        /// </summary>
        /// <param name="storage">Job storage</param>
        /// <param name="consoleId">Console identifier</param>
        /// <param name="start">Offset to read lines from</param>
        /// <remarks>
        /// On completion, <paramref name="start"/> is set to the end of the current batch, 
        /// and can be used for next requests (or set to -1, if the job has finished processing). 
        /// </remarks>
        private static IEnumerable<ConsoleLine> ReadLines(JobStorage storage, ConsoleId consoleId, ref int start)
        {
            if (start < 0) return null;

            using (var connection = (JobStorageConnection)storage.GetConnection())
            {
                var count = (int)connection.GetSetCount(consoleId.ToString());
                var result = new List<ConsoleLine>(Math.Max(1, count - start));

                if (count > start)
                {
                    // has some new items to fetch

                    Dictionary<string, ConsoleLine> progressBars = null;

                    var items = connection.GetRangeFromSet(consoleId.ToString(), start, count);
                    foreach (var item in items)
                    {
                        var entry = JobHelper.FromJson<ConsoleLine>(item);
                        
                        if (entry.ProgressValue.HasValue)
                        {
                            // aggregate progress value updates into single record

                            if (progressBars != null)
                            {
                                ConsoleLine prev;
                                if (progressBars.TryGetValue(entry.Message, out prev))
                                {
                                    prev.ProgressValue = entry.ProgressValue;
                                    prev.TextColor = entry.TextColor;
                                    continue;
                                }
                            }
                            else
                            {
                                progressBars = new Dictionary<string, Serialization.ConsoleLine>();
                            }

                            progressBars.Add(entry.Message, entry);
                        }

                        result.Add(entry);
                    }
                }
                
                if (count <= start || start == 0)
                {
                    // no new items or initial load, check if the job is still performing
                    
                    var state = connection.GetStateData(consoleId.JobId);
                    if (state == null)
                    {
                        // No state found for a job, probably it was deleted
                        count = -2;
                    }
                    else
                    {
                        if (!string.Equals(state.Name, ProcessingState.StateName, StringComparison.OrdinalIgnoreCase) ||
                            !consoleId.Equals(new ConsoleId(consoleId.JobId, JobHelper.DeserializeDateTime(state.Data["StartedAt"]))))
                        {
                            // Job state has changed (either not Processing, or another Processing with different console id)
                            count = -1;
                        }
                    }
                }
                
                start = count;
                return result;
            }
        }
    }
}
