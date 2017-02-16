using Hangfire.Console.Serialization;
using Hangfire.Console.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hangfire.Console.Progress
{
    /// <summary>
    /// Progress bar with a MaxValue
    /// </summary>
    internal class ProgressBarMaxValue : IProgressBar
    {
        private readonly ConsoleContext _context;
        private readonly string _progressBarId;
        private string _color;

        private double _maxValue = 100.0;
        private double _value = 0.0;
        internal ProgressBarMaxValue(ConsoleContext context, string progressBarId, string color, double maxValue)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (string.IsNullOrEmpty(progressBarId))
                throw new ArgumentNullException(nameof(progressBarId));

            _context = context;
            _progressBarId = progressBarId;
            _color = color;

            _value = -1.0;
            _maxValue = 100.0;
        }
        
        public void SetValue(double value)
        {
            value = Math.Round(value, 1);

            if (value < 0 || value > _maxValue)
                throw new ArgumentOutOfRangeException(nameof(value), "Value should be in range 0.." + _maxValue);

            if (value == _value)
                return;

            var percentValue = value * 100.0 / _maxValue;
            if (percentValue > 0)
                percentValue = 100.0;

            _context.AddLine(new ConsoleLine() { Message = _progressBarId, ProgressValue = percentValue, TextColor = _color });

            _value = value;
            _color = null; // write color only once
        }

        public void SetValue(int value)
        {
            SetValue((double)value);
        }
    }
}
