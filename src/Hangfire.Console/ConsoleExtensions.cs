using Hangfire.Console.Progress;
using Hangfire.Console.Server;
using Hangfire.Server;
using System;

namespace Hangfire.Console
{
    /// <summary>
    /// Provides extension methods for writing to console.
    /// </summary>
    public static class ConsoleExtensions
    {
        /// <summary>
        /// Sets text color for next console lines.
        /// </summary>
        /// <param name="context">Context</param>
        /// <param name="color">Text color to use</param>
        public static void SetTextColor(this PerformContext context, ConsoleTextColor color)
        {
            if (color == null)
                throw new ArgumentNullException(nameof(color));

            var consoleContext = ConsoleContext.FromPerformContext(context);
            if (consoleContext == null) return;

            consoleContext.TextColor = color;
        }

        /// <summary>
        /// Resets text color for next console lines.
        /// </summary>
        /// <param name="context">Context</param>
        public static void ResetTextColor(this PerformContext context)
        {
            var consoleContext = ConsoleContext.FromPerformContext(context);
            if (consoleContext == null) return;

            consoleContext.TextColor = null;
        }
        
        /// <summary>
        /// Adds an updateable progress bar to console.
        /// </summary>
        /// <param name="context">Context</param>
        /// <param name="value">Initial value</param>
        /// <param name="color">Progress bar color</param>
        public static IProgressBar WriteProgressBar(this PerformContext context, int value = 0, ConsoleTextColor color = null)
        {
            return ConsoleContext.FromPerformContext(context)?.WriteProgressBar(value, color) ?? new NoOpProgressBar();
        }

        /// <summary>
        /// Adds an updatable progress bar to console, with a max value
        /// </summary>
        /// <param name="context">Context</param>
        /// <param name="value">Initial value</param>
        /// <param name="maxValue">Maximum value</param>
        /// <param name="color">Progress bar color</param>
        public static IProgressBar WriteProgressBar(this PerformContext context, double value = 0.0, double maxValue = 100.0, ConsoleTextColor color = null)
        {
            return ConsoleContext.FromPerformContext(context)?.WriteProgressBar(value, maxValue, color) ?? new NoOpProgressBar();
        }

        /// <summary>
        /// Adds a string to console.
        /// </summary>
        /// <param name="context">Context</param>
        /// <param name="value">String</param>
        public static void WriteLine(this PerformContext context, string value)
        {
            ConsoleContext.FromPerformContext(context)?.WriteLine(value);
        }

        /// <summary>
        /// Adds an empty line to console.
        /// </summary>
        /// <param name="context">Context</param>
        public static void WriteLine(this PerformContext context)
            => WriteLine(context, "");

        /// <summary>
        /// Adds a value to a console.
        /// </summary>
        /// <param name="context">Context</param>
        /// <param name="value">Value</param>
        public static void WriteLine(this PerformContext context, object value)
            => WriteLine(context, value?.ToString());

        /// <summary>
        /// Adds a formatted string to a console.
        /// </summary>
        /// <param name="context">Context</param>
        /// <param name="format">Format string</param>
        /// <param name="arg0">Argument</param>
        public static void WriteLine(this PerformContext context, string format, object arg0)
            => WriteLine(context, string.Format(format, arg0));

        /// <summary>
        /// Adds a formatted string to a console.
        /// </summary>
        /// <param name="context">Context</param>
        /// <param name="format">Format string</param>
        /// <param name="arg0">Argument</param>
        /// <param name="arg1">Argument</param>
        public static void WriteLine(this PerformContext context, string format, object arg0, object arg1)
            => WriteLine(context, string.Format(format, arg0, arg1));

        /// <summary>
        /// Adds a formatted string to a console.
        /// </summary>
        /// <param name="context">Context</param>
        /// <param name="format">Format string</param>
        /// <param name="arg0">Argument</param>
        /// <param name="arg1">Argument</param>
        /// <param name="arg2">Argument</param>
        public static void WriteLine(this PerformContext context, string format, object arg0, object arg1, object arg2)
            => WriteLine(context, string.Format(format, arg0, arg1, arg2));

        /// <summary>
        /// Adds a formatted string to a console.
        /// </summary>
        /// <param name="context">Context</param>
        /// <param name="format">Format string</param>
        /// <param name="args">Arguments</param>
        public static void WriteLine(this PerformContext context, string format, params object[] args)
            => WriteLine(context, string.Format(format, args));
    }
}
