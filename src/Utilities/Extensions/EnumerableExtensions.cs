using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CloutCast
{
    public static class EnumerableExtensions
    {
        /* https://davefancher.com/2015/12/11/functional-c-chaining-async-methods/ */
        public static async Task<TResult> MapAsync<TSource, TResult>(this TSource @this,
            Func<TSource, Task<TResult>> fn) => await fn(@this);

        public static async Task<TResult> MapAsync<TSource, TResult>(this Task<TSource> @this,
            Func<TSource, TResult> fn) => fn(await @this);
        
        public static void ForEach<T>(this IEnumerable<T> enumeration, Action<T, int> action)
        {
            var i = 0;
            foreach (var e in enumeration) action(e, i++);
        }
        public static void ForEach<T>(this IEnumerable<T> enumeration, Action<T> action)
        {
            foreach (var item in enumeration) action(item);
        }

        public static IEnumerable<T> Iterate<T>(this IEnumerable<T> enumeration, Action<T> action)
        {
            if (enumeration == null) yield break;
            foreach (var item in enumeration)
            {
                action(item);
                yield return item;
            }
        }

        public static bool None<T>(this IEnumerable<T> source) => source == null || !source.Any();
        public static bool None<T>(this IEnumerable<T> source, Func<T, bool> predicate) => source == null || !source.Any(predicate);

        public static IEnumerable<(T item, int index)> WithIndex<T>(this IEnumerable<T> source) 
            => source.Select((item, index) => (item, index));
        public static string ToCommaDelimitedString<T>(this IEnumerable<T> source, bool wrapInSingleQuote = true, Func<T, string> converter = null)
            => ToDelimitedString(source, ", ", wrapInSingleQuote, converter);

        public static string ToDelimitedString<T>(this IEnumerable<T> source, string delimiter, bool wrapInSingleQuote = true, Func<T, string> converter = null)
        {
            converter = converter ?? (x => x.ToString());

            return wrapInSingleQuote
                ? string.Join(delimiter, source.Select(e => $"'{converter(e)}'"))
                : string.Join(delimiter, source.Select(e => converter(e)));
        }


    }
}