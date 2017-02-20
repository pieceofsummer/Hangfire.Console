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
        private double _value;
        private double _maxValue;

        internal DefaultProgressBar(ConsoleContext context, string progressBarId, string color, double maxValue)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (string.IsNullOrEmpty(progressBarId))
                throw new ArgumentNullException(nameof(progressBarId));
               
            _context = context;
            _progressBarId = progressBarId;
            _color = color;
            _value = -1;
            _maxValue = maxValue;
        }

        public void SetValue(int value)
        {
            SetValue((double)value);
        }

        public void SetValue(double value)
        {
            value = Math.Round(value, 1);

            if (value < 0 || value > _maxValue)
                throw new ArgumentOutOfRangeException(nameof(value), "Value should be in range 0.." + _maxValue);

            if (_value == value) return;

            var percentValue = value * 100.0 / _maxValue;

            _context.AddLine(new ConsoleLine() { Message = _progressBarId, ProgressValue = percentValue, TextColor = _color });

            _value = value;
            _color = null; // write color only once
        }
    }
}
