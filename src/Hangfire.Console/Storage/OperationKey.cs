using System;
using Hangfire.Console.Serialization;

namespace Hangfire.Console.Storage
{
    /// <summary>
    /// 
    /// </summary>
    internal class OperationKey : IEquatable<OperationKey>
    {
        /// <inheritdoc />
        public OperationKey(ConsoleId consoleId, string progressBarId = null)
        {
            ConsoleId = consoleId ?? throw new ArgumentNullException(nameof(consoleId));
            ProgressBarId = progressBarId;
        }

        /// <summary>
        /// 
        /// </summary>
        public ConsoleId ConsoleId { get; }
        
        /// <summary>
        /// 
        /// </summary>
        public string ProgressBarId { get; }

        /// <inheritdoc />
        public bool Equals(OperationKey other)
        {
            if (ReferenceEquals(other, null)) return false;
            if (ReferenceEquals(other, this)) return true;

            return ConsoleId == other.ConsoleId &&
                   string.Equals(ProgressBarId, other.ProgressBarId, StringComparison.Ordinal);
        }

        /// <inheritdoc />
        public override bool Equals(object obj) => Equals(obj as OperationKey);

        /// <inheritdoc />
        public override int GetHashCode() => ConsoleId.GetHashCode() ^ (107 * ProgressBarId?.GetHashCode() ?? 0);

        /// <inheritdoc />
        public override string ToString() => $"{ConsoleId}:{ProgressBarId}";

        /// <summary>
        /// 
        /// </summary>
        public static bool operator ==(OperationKey x, OperationKey y) => x?.Equals(y) ?? ReferenceEquals(y, null);

        /// <summary>
        /// 
        /// </summary>
        public static bool operator !=(OperationKey x, OperationKey y) => !(x == y);
    }
}