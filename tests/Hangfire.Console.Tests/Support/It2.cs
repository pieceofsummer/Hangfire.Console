using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Moq;

// ReSharper disable once CheckNamespace
namespace Hangfire.Console.Tests
{
    public static class It2
    {
        public static IEnumerable<TValue> AnyIs<TValue>(Expression<Func<TValue, bool>> match)
        {
            var predicate = match.Compile();

            return Match.Create<IEnumerable<TValue>>(
                values => values.Any(predicate),
                () => It2.AnyIs<TValue>(match));
        }
    }
}