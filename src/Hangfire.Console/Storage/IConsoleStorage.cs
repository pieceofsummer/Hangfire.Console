using Hangfire.Console.Serialization;
using Hangfire.Storage;
using System;
using System.Collections.Generic;

namespace Hangfire.Console.Storage
{
    /// <summary>
    /// Abstraction over Hangfire's storage API
    /// </summary>
    internal interface IConsoleStorage : IDisposable
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
        /// Initializes console.
        /// </summary>
        /// <param name="consoleId">Console identifier</param>
        void InitConsole(ConsoleId consoleId);

        /// <summary>
        /// Adds line to console.
        /// </summary>
        /// <param name="consoleId">Console identifier</param>
        /// <param name="line">Line to add</param>
        void AddLine(ConsoleId consoleId, ConsoleLine line);

        /// <summary>
        /// Returns current expiration TTL for console.
        /// If console is not expired, returns negative <see cref="TimeSpan"/>.
        /// </summary>
        /// <param name="consoleId">Console identifier</param>
        TimeSpan GetConsoleTtl(ConsoleId consoleId);

        /// <summary>
        /// Expire data for console.
        /// </summary>
        /// <param name="consoleId">Console identifier</param>
        /// <param name="expireIn">Expiration time</param>
        void Expire(ConsoleId consoleId, TimeSpan expireIn);
        
        /// <summary>
        /// Returns last (current) state of the console's parent job.
        /// </summary>
        /// <param name="consoleId">Console identifier</param>
        StateData GetState(ConsoleId consoleId);

        /// <summary>
        /// Returns overall progress of the console (if available).
        /// </summary>
        /// <param name="consoleId">Console identifier</param>
        /// <returns>Progress value</returns>
        double? GetProgress(ConsoleId consoleId);
    }
}
