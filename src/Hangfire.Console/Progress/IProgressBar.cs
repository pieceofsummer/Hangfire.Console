namespace Hangfire.Console.Progress
{
    /// <summary>
    /// Progress bar line inside console.
    /// </summary>
    public interface IProgressBar
    {
        /// <summary>
        /// Updates a value of a progress bar.
        /// </summary>
        /// <param name="value">New value</param>
        void SetValue(int value);

        /// <summary>
        /// Updates a value of a progress bar.
        /// </summary>
        /// <param name="value">New value</param>
        void SetValue(double value);
    }
}
