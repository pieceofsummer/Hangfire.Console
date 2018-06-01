using Hangfire.Dashboard;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Hangfire.Console.Dashboard
{
    /// <summary>
    /// Dispatcher for configured styles
    /// </summary>
    internal class DynamicCssDispatcher : IDashboardDispatcher
    {
        private readonly ConsoleOptions _options;

        public DynamicCssDispatcher(ConsoleOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }
        
        public Task Dispatch(DashboardContext context)
        {
            var builder = new StringBuilder();

            builder.AppendLine(".console, .console .line-buffer {")
                   .Append("    background-color: ").Append(_options.BackgroundColor).AppendLine(";")
                   .Append("    color: ").Append(_options.TextColor).AppendLine(";")
                   .AppendLine("}");

            builder.AppendLine(".console .line > span[data-moment-title] {")
                   .Append("    color: ").Append(_options.TimestampColor).AppendLine(";")
                   .AppendLine("}");

            builder.AppendLine(".console .line > a, .console.line > a:visited, .console.line > a:hover {")
                   .Append("    color: ").Append(_options.TextColor).AppendLine(";")
                   .AppendLine("}");

            builder.AppendLine(".console .line.pb > .pv:before {")
                   .Append("    color: ").Append(_options.BackgroundColor).AppendLine(";")
                   .AppendLine("}");
            
            return context.Response.WriteAsync(builder.ToString());
        }
    }
}
