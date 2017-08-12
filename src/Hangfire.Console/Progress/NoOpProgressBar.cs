using System;

namespace Hangfire.Console.Progress
{
    /// <summary>
    /// No-op progress bar.
    /// </summary>
    internal class NoOpProgressBar : IProgressBar
    {
        private double _maxValue = 100.0;
        public NoOpProgressBar(double maxValue)
        {
            _maxValue = maxValue;
        }

        public NoOpProgressBar() { }

        public void SetValue(int value)
        {
            SetValue((double)value);
        }

        public void SetValue(double value)
        {
            value = Math.Round(value, 1);

            if (value < 0 || value > _maxValue)
                throw new ArgumentOutOfRangeException(nameof(value), "Value should be in range 0.." + _maxValue);
        }
    }
}
