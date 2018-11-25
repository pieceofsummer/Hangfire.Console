using System;
using System.Collections.Generic;
using Hangfire.Console.Serialization;
using Hangfire.Storage;

namespace Hangfire.Console.Storage
{
    /// <summary>
    /// Minimal API set for read operations
    /// </summary>
    internal interface IConsoleStorageRead : IDisposable
    {
        /// <summary>
        /// Returns number of lines for console.
        /// </summary>
        /// <param name="consoleId">Console identifier</param>
        int GetLineCount(ConsoleId consoleId);

        /// <summary>
        /// Returns range of lines for console.
        /// </summary>
        /// <param name="consoleId">Console identifier</param>
        /// <param name="start">Start index (inclusive)</param>
        /// <param name="end">End index (inclusive)</param>
        IEnumerable<ConsoleLine> GetLines(ConsoleId consoleId, int start, int end);

        /// <summary>
        /// Returns current state of the job.
        /// </summary>
        /// <param name="jobId">Job identifier</param>
        StateData GetState(string jobId);

        /// <summary>
        /// Returns overall progress of the console (if available).
        /// </summary>
        /// <param name="consoleId">Console identifier</param>
        /// <returns>Progress value</returns>
        double? GetProgress(ConsoleId consoleId);
    }
}