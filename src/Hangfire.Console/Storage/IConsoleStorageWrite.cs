using System;
using Hangfire.Console.Serialization;

namespace Hangfire.Console.Storage
{
    /// <summary>
    /// Minimal API set for write operations
    /// </summary>
    internal interface IConsoleStorageWrite
    {
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
    }
}