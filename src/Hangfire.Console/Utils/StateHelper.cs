using System;
using Hangfire.States;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;

namespace Hangfire.Console.Utils
{
    /// <summary>
    /// Provides helper methods to check if state is actually <see cref="ProcessingState"/>.
    /// </summary>
    internal static class StateHelper
    {
        public static bool IsProcessingState(string stateName)
        {
            return string.Equals(stateName, ProcessingState.StateName, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsProcessingState(this StateData state) => state != null && IsProcessingState(state.Name);

        public static bool IsProcessingState(this StateHistoryDto state) => state != null && IsProcessingState(state.StateName);
    }
}