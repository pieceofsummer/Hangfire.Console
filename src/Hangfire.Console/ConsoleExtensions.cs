using Hangfire.Common;
using Hangfire.Console.Serialization;
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
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (color == null)
                throw new ArgumentNullException(nameof(color));

            context.Items["ConsoleTextColor"] = color;
        }

        /// <summary>
        /// Resets text color for next console lines.
        /// </summary>
        /// <param name="context">Context</param>
        public static void ResetTextColor(this PerformContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            context.Items.Remove("ConsoleTextColor");
        }
        
        /// <summary>
        /// Adds a string to console.
        /// </summary>
        /// <param name="context">Context</param>
        /// <param name="value">String</param>
        public static void WriteLine(this PerformContext context, string value)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            
            if (!context.Items.ContainsKey("ConsoleId"))
            {
                // Absence of ConsoleId means ConsoleServerFilter was not properly added
                return;
            }
            
            var consoleId = (ConsoleId)context.Items["ConsoleId"];

            var line = new ConsoleLine();
            line.TimeOffset = Math.Round((DateTime.UtcNow - consoleId.DateValue).TotalSeconds, 6);
            line.Message = value ?? "";

            if (context.Items.ContainsKey("ConsoleTextColor"))
            {
                line.TextColor = context.Items["ConsoleTextColor"].ToString();
            }

            using (var tran = context.Connection.CreateWriteTransaction())
            {
                tran.AddToSet(consoleId.ToString(), JobHelper.ToJson(line));
                tran.Commit();
            }
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
