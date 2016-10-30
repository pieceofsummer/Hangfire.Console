using Hangfire.Console.Serialization;
using Hangfire.Console.Server;
using System;

namespace Hangfire.Console.Progress
{
    /// <summary>
    /// Default progress bar.
    /// </summary>
    internal class DefaultProgressBar : IProgressBar
    {
        private readonly ConsoleContext _context;
        private readonly string _progressBarId;
        private string _color;
        private int _value;

        internal DefaultProgressBar(ConsoleContext context, string progressBarId, string color)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (string.IsNullOrEmpty(progressBarId))
                throw new ArgumentNullException(nameof(progressBarId));
               
            _context = context;
            _progressBarId = progressBarId;
            _color = color;
            _value = -1;
        }

        public void SetValue(int value)
        {
            if (value < 0 || value > 100)
                throw new ArgumentOutOfRangeException(nameof(value), "Value should be in range 0..100");

            if (_value == value) return;
            
            _context.AddLine(new ConsoleLine() { Message = _progressBarId, ProgressValue = value, TextColor = _color });

            _value = value;
            _color = null; // write color only once
        }
    }
}
