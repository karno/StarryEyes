using System.Collections.Generic;
using JetBrains.Annotations;

// ReSharper disable CheckNamespace

namespace System.Linq
// ReSharper restore CheckNamespace
{
    public static class EnumerableFx
    {
        private static readonly Random _rng = new Random();

        public static IEnumerable<T> Append<T>([NotNull] this IEnumerable<T> source, [NotNull] params T[] item)
        {
            return source.Concat(item);
        }

        public static IOrderedEnumerable<TSource> Shuffle<TSource>([NotNull] this IEnumerable<TSource> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return source.OrderBy(_ => _rng.Next());
        }

        public static IEnumerable<T> WhereDo<T>([NotNull] this IEnumerable<T> source, [NotNull] Func<T, bool> condition,
           [NotNull] Action<T> passed)
        {
            foreach (var v in source)
            {
                if (condition(v))
                    passed(v);
                yield return v;
            }
        }

        public static IEnumerable<T> Steal<T>([NotNull] this IEnumerable<T> source, [NotNull] Func<T, bool> condition,
           [NotNull] Action<T> passed)
        {
            foreach (var v in source)
            {
                if (condition(v))
                    passed(v);
                else
                    yield return v;
            }
        }

        public static IEnumerable<T> Guard<T>([CanBeNull] this IEnumerable<T> source)
        {
            return source ?? Enumerable.Empty<T>();
        }

        public static IEnumerable<T> Guard<T>([CanBeNull] this IEnumerable<T> source, [NotNull] Func<bool> guard)
        {
            return guard() ? source.Guard() : Enumerable.Empty<T>();
        }

        public static IEnumerable<T> Guard<T>([CanBeNull] this IEnumerable<T> source,
           [NotNull] Func<IEnumerable<T>, bool> guard)
        {
            var buffer = source.Guard().Memoize();
            return guard(buffer) ? buffer : Enumerable.Empty<T>();
        }

        public static IEnumerable<TResult> Singlize<TSource, TIntermediate, TResult>(
            [NotNull] this IEnumerable<TSource> source, [NotNull] Func<IEnumerable<TSource>, TIntermediate> generator,
            [NotNull] Func<TSource, TIntermediate, TResult> mapper)
        {
            var cached = source.Share();
            var i = generator(cached);
            return cached.Select(s => mapper(s, i));
        }

        public static TResult Using<T, TResult>(this T disposable, [NotNull] Func<T, TResult> func) where T : IDisposable
        {
            using (disposable) return func(disposable);
        }

        public static string JoinString([NotNull] this IEnumerable<string> source, [NotNull] string separator)
        {
            return String.Join(separator, source);
        }

        public static IEnumerable<T> TakeIfNotNull<T>([NotNull] this IEnumerable<T> source, int? take)
        {
            return take == null ? source : source.Take(take.Value);
        }
    }
}
