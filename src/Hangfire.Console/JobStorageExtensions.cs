using Hangfire.Console.Monitoring;
using System;

namespace Hangfire.Console
{
    /// <summary>
    /// Provides extension methods for <see cref="JobStorage"/>.
    /// </summary>
    public static class JobStorageExtensions
    {
        /// <summary>
        /// Returns an instance of <see cref="IConsoleApi"/>.
        /// </summary>
        /// <param name="storage">Job storage instance</param>
        /// <returns>Console API instance</returns>
        public static IConsoleApi GetConsoleApi(this JobStorage storage)
        {
            if (storage == null)
                throw new ArgumentNullException(nameof(storage));

            return new ConsoleApi(storage.GetConnection());
        }
    }
}
