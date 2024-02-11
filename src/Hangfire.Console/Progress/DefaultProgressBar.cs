using Hangfire.Console.Serialization;
using Hangfire.Console.Server;
using System;
using System.Threading;

namespace Hangfire.Console.Progress
{
    /// <summary>
    /// Default progress bar.
    /// </summary>
    internal class DefaultProgressBar : IProgressBar
    {
        private readonly ConsoleContext _context;
        private readonly string _progressBarId;
        private readonly int _decimalDigits;
        private string _name;
        private string _color;
        private double _value;

        internal DefaultProgressBar(ConsoleContext context, string progressBarId, int decimalDigits, string name, string color)
        {
            if (string.IsNullOrEmpty(progressBarId))
                throw new ArgumentNullException(nameof(progressBarId));
            if (decimalDigits < 0 || decimalDigits > 3)
                throw new ArgumentOutOfRangeException(nameof(decimalDigits), "Number of decimal digits should be between 0 and 3");

            _context = context ?? throw new ArgumentNullException(nameof(context));
            _progressBarId = progressBarId;
            _decimalDigits = decimalDigits;
            _name = name;
            _color = color;
            _value = -1;
        }

        public void SetValue(int value)
        {
            SetValue((double)value);
        }

        public void SetValue(double value)
        {
            value = Math.Round(value, _decimalDigits);

            if (value < 0 || value > 100)
                throw new ArgumentOutOfRangeException(nameof(value), "Value should be in range 0..100");

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (Interlocked.Exchange(ref _value, value) == value) return;
            
            _context.AddLine(new ConsoleLine() { Message = _progressBarId, ProgressName = _name, ProgressValue = value, TextColor = _color });

            _name = String.IsNullOrEmpty(_name) ? null : _name;
            _color = String.IsNullOrEmpty(_color) ? null : _color;
        }
    }
}
