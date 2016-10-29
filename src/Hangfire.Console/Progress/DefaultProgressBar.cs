using Hangfire.Console.Serialization;
using Hangfire.Server;
using System;

namespace Hangfire.Console.Progress
{
    /// <summary>
    /// Default progress bar.
    /// </summary>
    internal class DefaultProgressBar : IProgressBar
    {
        private readonly PerformContext _context;
        private readonly ConsoleId _consoleId;
        private readonly string _progressBarId;
        private readonly string _color;
        private int _value;

        internal DefaultProgressBar(PerformContext context, ConsoleId consoleId, string color)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (consoleId == null)
                throw new ArgumentNullException(nameof(consoleId));
            
            _context = context;
            _consoleId = consoleId;
            _color = color;
            _value = -1;

            // generate next progress bar id
            int id = context.Items.ContainsKey("ConsoleProgressBarId") 
                ? (int)context.Items["ConsoleProgressBarId"] + 1
                : 0;

            _progressBarId = id.ToString();
            context.Items["ConsoleProgressBarId"] = id;
        }

        public void SetValue(int value)
        {
            if (value < 0 || value > 100)
                throw new ArgumentOutOfRangeException(nameof(value), "Value should be in range 0..100");

            if (_value == value) return;

            var line = new ConsoleLine() { Message = _progressBarId, ProgressValue = value, TextColor = _color };
            
            _context.AddLine(_consoleId, line);

            _value = value;
        }
    }
}
