using Hangfire.Dashboard;
using System.Threading.Tasks;
using Hangfire.Annotations;
using System.Text;
using Hangfire.Console.Serialization;
using System;

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
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            _options = options;
        }

        public Task Dispatch([NotNull] DashboardContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var consoleId = ConsoleId.Parse(context.UriMatch.Groups[1].Value);

            var startArg = context.Request.GetQuery("start");

            // try to parse offset at which we should start returning requests
            int start;
            if (string.IsNullOrEmpty(startArg) || !int.TryParse(startArg, out start))
            {
                // if not provided or invalid, fetch records from the very start
                start = 0;
            }
            
            var buffer = new StringBuilder();
            ConsoleRenderer.RenderConsole(buffer, context.Storage, consoleId, start);
            
            context.Response.ContentType = "text/html";
            return context.Response.WriteAsync(buffer.ToString());
        }
    }
}
