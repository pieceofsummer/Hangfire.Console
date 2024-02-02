using Hangfire.Dashboard;
using Hangfire.Console.Storage;
using Hangfire.Console.Serialization;
using System;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;

namespace Hangfire.Console.Dashboard
{
    /// <summary>
    /// Provides incremental updates for a console. 
    /// </summary>
    internal class ConsoleDispatcher : IDashboardDispatcher
    {
        private readonly ConsoleOptions _options;

        public ConsoleDispatcher(ConsoleOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public Task Dispatch(DashboardContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var consoleId = ConsoleId.Parse(context.UriMatch.Groups[1].Value);

            var startArg = context.Request.GetQuery("start");

            // try to parse offset at which we should start returning requests
            if (string.IsNullOrEmpty(startArg) || !int.TryParse(startArg, NumberStyles.Integer, CultureInfo.InvariantCulture, out var start))
            {
                // if not provided or invalid, fetch records from the very start
                start = 0;
            }

            var buffer = new StringBuilder();
            using (var storage = new ConsoleStorage(context.Storage.GetConnection()))
            {
                ConsoleRenderer.RenderLineBuffer(buffer, storage, consoleId, start);
            }

            context.Response.ContentType = "text/html";
            return context.Response.WriteAsync(buffer.ToString());
        }
    }
}
