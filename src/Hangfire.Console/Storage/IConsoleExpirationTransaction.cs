using Hangfire.Console.Serialization;
using System;

namespace Hangfire.Console.Storage
{
    internal interface IConsoleExpirationTransaction : IDisposable
    {
        /// <summary>
        /// Expire data for console.
        /// </summary>
        /// <param name="consoleId">Console identifier</param>
        /// <param name="expireIn">Expiration time</param>
        void Expire(ConsoleId consoleId, TimeSpan expireIn);

        /// <summary>
        /// Persist data for console.
        /// </summary>
        /// <param name="consoleId">Console identifier</param>
        void Persist(ConsoleId consoleId);
    }
}
