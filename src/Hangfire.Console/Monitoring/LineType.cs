namespace Hangfire.Console.Monitoring
{
    /// <summary>
    /// Console line type
    /// </summary>
    public enum LineType
    {
        /// <summary>
        /// Any type (only for filtering)
        /// </summary>
        Any,
        /// <summary>
        /// Textual line
        /// </summary>
        Text,
        /// <summary>
        /// Progress bar
        /// </summary>
        ProgressBar
    }
}
