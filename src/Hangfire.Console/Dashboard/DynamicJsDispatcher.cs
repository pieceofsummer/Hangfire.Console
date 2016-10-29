using Hangfire.Dashboard;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Hangfire.Console.Dashboard
{
    /// <summary>
    /// Dispatcher for configured script
    /// </summary>
    internal class DynamicJsDispatcher : IDashboardDispatcher
    {
        private readonly ConsoleOptions _options;

        public DynamicJsDispatcher(ConsoleOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            _options = options;
        }
        
        public Task Dispatch(DashboardContext context)
        {
            var builder = new StringBuilder();

            builder.Append(@"(function (hangFire) {")
                   .Append("hangFire.config = hangFire.config || {};")
                   .AppendFormat("hangFire.config.consolePollInterval = {0};", _options.PollInterval)
                   .AppendFormat("hangFire.config.consolePollUrl = '{0}/console/';", context.Request.PathBase)
                   .Append("})(window.Hangfire = window.Hangfire || {});")
                   .AppendLine();

            return context.Response.WriteAsync(builder.ToString());
        }
    }
}
