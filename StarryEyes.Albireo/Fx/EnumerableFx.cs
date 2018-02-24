using System.Collections.Generic;
using JetBrains.Annotations;

// ReSharper disable CheckNamespace

namespace System.Linq
// ReSharper restore CheckNamespace
{
    public static class EnumerableFx
    {
        private static readonly Random _rng = new Random();

        public static IEnumerable<T> Append<T>([CanBeNull] this IEnumerable<T> source, [CanBeNull] params T[] item)
        {
            return source.Concat(item);
        }

        public static IOrderedEnumerable<TSource> Shuffle<TSource>([CanBeNull] this IEnumerable<TSource> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return source.OrderBy(_ => _rng.Next());
        }

        public static IEnumerable<T> WhereDo<T>([CanBeNull] this IEnumerable<T> source, [CanBeNull] Func<T, bool> condition,
           [CanBeNull] Action<T> passed)
        {
            foreach (var v in source)
            {
                if (condition(v))
                    passed(v);
                yield return v;
            }
        }

        public static IEnumerable<T> Steal<T>([CanBeNull] this IEnumerable<T> source, [CanBeNull] Func<T, bool> condition,
           [CanBeNull] Action<T> passed)
        {
            foreach (var v in source)
            {
                if (condition(v))
                    passed(v);
                else
                    yield return v;
            }
        }

        public static IEnumerable<T> Guard<T>([CanBeNullAttribute] this IEnumerable<T> source)
        {
            return source ?? Enumerable.Empty<T>();
        }

        public static IEnumerable<T> Guard<T>([CanBeNullAttribute] this IEnumerable<T> source, [CanBeNull] Func<bool> guard)
        {
            return guard() ? source.Guard() : Enumerable.Empty<T>();
        }

        public static IEnumerable<T> Guard<T>([CanBeNullAttribute] this IEnumerable<T> source,
           [CanBeNull] Func<IEnumerable<T>, bool> guard)
        {
            var buffer = source.Guard().Memoize();
            return guard(buffer) ? buffer : Enumerable.Empty<T>();
        }

        public static IEnumerable<TResult> Singlize<TSource, TIntermediate, TResult>(
            [CanBeNull] this IEnumerable<TSource> source, [CanBeNull] Func<IEnumerable<TSource>, TIntermediate> generator,
            [CanBeNull] Func<TSource, TIntermediate, TResult> mapper)
        {
            var cached = source.Share();
            var i = generator(cached);
            return cached.Select(s => mapper(s, i));
        }

        public static TResult Using<T, TResult>(this T disposable, [CanBeNull] Func<T, TResult> func) where T : IDisposable
        {
            using (disposable) return func(disposable);
        }

        public static string JoinString([CanBeNull] this IEnumerable<string> source, [CanBeNull] string separator)
        {
            return String.Join(separator, source);
        }

        public static IEnumerable<T> TakeIfNotNull<T>([CanBeNull] this IEnumerable<T> source, int? take)
        {
            return take == null ? source : source.Take(take.Value);
        }
    }
}