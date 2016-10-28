using Hangfire.Console.Dashboard;
using Hangfire.Console.Server;
using Hangfire.Dashboard;
using Hangfire.Dashboard.Extensions;
using Hangfire.States;
using System;
using System.Reflection;

namespace Hangfire.Console
{
    /// <summary>
    /// Provides extension methods to setup Hangfire.Console.
    /// </summary>
    public static class GlobalConfigurationExtensions
    {
        /// <summary>
        /// Configures Hangfire to use Console.
        /// </summary>
        /// <param name="configuration">Global configuration</param>
        /// <param name="options">Options for console</param>
        public static IGlobalConfiguration UseConsole(this IGlobalConfiguration configuration, ConsoleOptions options = null)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            options = options ?? new ConsoleOptions();

            // register server filter for jobs
            GlobalJobFilters.Filters.Add(new ConsoleServerFilter(options));

            // replace renderer for Processing state
            JobHistoryRenderer.Register(ProcessingState.StateName, new ProcessingStateRenderer(options).Render);

            // register dispatcher to serve console updates
            DashboardRoutes.Routes.Add("/console/([0-9a-f]{11}.+)", new ConsoleDispatcher(options));

            // register additional dispatchers for CSS and JS
            var assembly = typeof(ConsoleRenderer).GetTypeInfo().Assembly;
            DashboardRoutes.Routes.Append("/js[0-9]{3}", new EmbeddedResourceDispatcher(assembly, "Hangfire.Console.Resources.resize.min.js"));
            DashboardRoutes.Routes.Append("/js[0-9]{3}", new EmbeddedResourceDispatcher(assembly, "Hangfire.Console.Resources.script.js"));
            DashboardRoutes.Routes.Append("/css[0-9]{3}", new EmbeddedResourceDispatcher(assembly, "Hangfire.Console.Resources.style.css"));
            
            return configuration;
        }
    }
}
