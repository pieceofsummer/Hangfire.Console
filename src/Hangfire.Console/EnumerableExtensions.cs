using Hangfire.Console.Progress;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Hangfire.Console
{
    /// <summary>
    /// Provides a set of extension methods to enumerate collections with progress.
    /// </summary>
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Returns a <see cref="IEnumerable{T}"/> reporting enumeration progress.
        /// </summary>
        /// <typeparam name="T">Item type</typeparam>
        /// <param name="enumerable">Source enumerable</param>
        /// <param name="progressBar">Progress bar</param>
        /// <param name="count">Item count</param>
        public static IEnumerable<T> WithProgress<T>(this IEnumerable<T> enumerable, IProgressBar progressBar, int count = -1)
        {
            if (enumerable is ICollection<T>)
            {
                count = ((ICollection<T>)enumerable).Count;
            }
            else if (enumerable is IReadOnlyCollection<T>)
            {
                count = ((IReadOnlyCollection<T>)enumerable).Count;
            }
            else if (count < 0)
            {
                throw new ArgumentException("Count is required when enumerable is not a collection", nameof(count));
            }

            return new ProgressEnumerable<T>(enumerable, progressBar, count);
        }

        /// <summary>
        /// Returns a <see cref="IEnumerable"/> reporting enumeration progress.
        /// </summary>
        /// <param name="enumerable">Source enumerable</param>
        /// <param name="progressBar">Progress bar</param>
        /// <param name="count">Item count</param>
        public static IEnumerable WithProgress(this IEnumerable enumerable, IProgressBar progressBar, int count = -1)
        {
            if (enumerable is ICollection)
            {
                count = ((ICollection)enumerable).Count;
            }
            else if (count < 0)
            {
                throw new ArgumentException("Count is required when enumerable is not a collection", nameof(count));
            }

            return new ProgressEnumerable(enumerable, progressBar, count);
        }

        /// <summary>
        /// Returns a <see cref="IEnumerable{T}"/> reporting enumeration progress.
        /// </summary>
        /// <typeparam name="T">Item type</typeparam>
        /// <param name="enumerable">Source enumerable</param>
        /// <param name="progressBar">Progress bar</param>
        /// <param name="percentageIncrementStep">Minimum percentage step, controls frequency of updates</param>
        /// <param name="count">Item count</param>
        /// <returns></returns>
        public static IEnumerable<T> WithProgressIncrements<T>(this IEnumerable<T> enumerable, IProgressBar progressBar, int percentageIncrementStep, int count = -1)
        {
            if (enumerable is ICollection<T>)
            {
                count = ((ICollection<T>)enumerable).Count;
            }
            else if (enumerable is IReadOnlyCollection<T>)
            {
                count = ((IReadOnlyCollection<T>)enumerable).Count;
            }
            else if (count < 0)
            {
                throw new ArgumentException("Count is required when enumerable is not a collection", nameof(count));
            }

            return new ProgressEnumerable<T>(enumerable, progressBar, count, percentageIncrementStep);
        }

        /// <summary>
        /// Returns a <see cref="IEnumerable"/> reporting enumeration progress.
        /// </summary>
        /// <param name="enumerable">Source enumerable</param>
        /// <param name="progressBar">Progress bar</param>
        /// <param name="percentageIncrementStep">Minimum percentage step, controls frequency of updates</param>
        /// <param name="count">Item count</param>
        public static IEnumerable WithProgressIncrements(this IEnumerable enumerable, IProgressBar progressBar, int percentageIncrementStep, int count = -1)
        {
            if (enumerable is ICollection)
            {
                count = ((ICollection)enumerable).Count;
            }
            else if (count < 0)
            {
                throw new ArgumentException("Count is required when enumerable is not a collection", nameof(count));
            }

            return new ProgressEnumerable(enumerable, progressBar, count, percentageIncrementStep);
        }
    }
}
