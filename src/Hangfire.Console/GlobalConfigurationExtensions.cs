using Hangfire.Console.Dashboard;
using Hangfire.Console.Server;
using Hangfire.Console.States;
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

            options.Validate(nameof(options));

            if (DashboardRoutes.Routes.Contains("/console/([0-9a-f]{11}.+)"))
                throw new InvalidOperationException("Console is already initialized");

            // register server filter for jobs
            GlobalJobFilters.Filters.Add(new ConsoleServerFilter(options));

            // register apply state filter for jobs
            // (context may be altered by other state filters, so make it the very last filter in chain to use final context values)
            GlobalJobFilters.Filters.Add(new ConsoleApplyStateFilter(options), int.MaxValue);

            // replace renderer for Processing state
            JobHistoryRenderer.Register(ProcessingState.StateName, new ProcessingStateRenderer(options).Render);

            // register dispatchers to serve console data
            DashboardRoutes.Routes.Add("/console/progress", new JobProgressDispatcher(options));
            DashboardRoutes.Routes.Add("/console/([0-9a-f]{11}.+)", new ConsoleDispatcher(options));
            
            // register additional dispatchers for CSS and JS
            var assembly = typeof(ConsoleRenderer).GetTypeInfo().Assembly;
            
            var jsPath = DashboardRoutes.Routes.Contains("/js[0-9]+") ? "/js[0-9]+" : "/js[0-9]{3}";
            DashboardRoutes.Routes.Append(jsPath, new EmbeddedResourceDispatcher(assembly, "Hangfire.Console.Resources.resize.min.js"));
            DashboardRoutes.Routes.Append(jsPath, new DynamicJsDispatcher(options));
            DashboardRoutes.Routes.Append(jsPath, new EmbeddedResourceDispatcher(assembly, "Hangfire.Console.Resources.script.js"));

            var cssPath = DashboardRoutes.Routes.Contains("/css[0-9]+") ? "/css[0-9]+" : "/css[0-9]{3}";
            DashboardRoutes.Routes.Append(cssPath, new EmbeddedResourceDispatcher(assembly, "Hangfire.Console.Resources.style.css"));
            DashboardRoutes.Routes.Append(cssPath, new DynamicCssDispatcher(options));
            
            return configuration;
        }
    }
}
