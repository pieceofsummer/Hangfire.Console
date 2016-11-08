using System;

namespace Hangfire.Console.Progress
{
    /// <summary>
    /// No-op progress bar.
    /// </summary>
    internal class NoOpProgressBar : IProgressBar
    {
        public void SetValue(int value)
        {
            SetValue((double)value);
        }

        public void SetValue(double value)
        {
            value = Math.Round(value, 1);

            if (value < 0 || value > 100)
                throw new ArgumentOutOfRangeException(nameof(value), "Value should be in range 0..100");
        }
    }
}
