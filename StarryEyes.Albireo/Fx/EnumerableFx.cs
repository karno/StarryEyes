using System.Collections.Generic;

// ReSharper disable CheckNamespace
namespace System.Linq
// ReSharper restore CheckNamespace
{
    public static class EnumerableFx
    {
        private static readonly Random _rng = new Random();

        public static IEnumerable<T> Append<T>(this IEnumerable<T> source, params T[] item)
        {
            return source.Concat(item);
        }

        public static IOrderedEnumerable<TSource> Shuffle<TSource>(this IEnumerable<TSource> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return source.OrderBy(_ => _rng.Next());
        }

        public static IEnumerable<T> WhereDo<T>(this IEnumerable<T> source, Func<T, bool> condition, Action<T> passed)
        {
            foreach (var v in source)
            {
                if (condition(v))
                    passed(v);
                yield return v;
            }
        }

        public static IEnumerable<T> Steal<T>(this IEnumerable<T> source, Func<T, bool> condition, Action<T> passed)
        {
            foreach (var v in source)
            {
                if (condition(v))
                    passed(v);
                else
                    yield return v;
            }
        }

        public static IEnumerable<T> Guard<T>(this IEnumerable<T> source)
        {
            return source ?? Enumerable.Empty<T>();
        }

        public static IEnumerable<T> Guard<T>(this IEnumerable<T> source, Func<bool> guard)
        {
            return guard() ? source : Enumerable.Empty<T>();
        }

        public static IEnumerable<T> Guard<T>(this IEnumerable<T> source, Func<IEnumerable<T>, bool> guard)
        {
            var buffer = source.Memoize();
            return guard(buffer) ? buffer : Enumerable.Empty<T>();
        }

        public static IEnumerable<TResult> Singlize<TSource, TIntermediate, TResult>(this IEnumerable<TSource> source,
            Func<IEnumerable<TSource>, TIntermediate> generator, Func<TSource, TIntermediate, TResult> mapper)
        {
            var cached = source.Share();
            var i = generator(cached);
            return cached.Select(s => mapper(s, i));
        }

        public static TResult Using<T, TResult>(this T disposable, Func<T, TResult> func) where T : IDisposable
        {
            using (disposable) return func(disposable);
        }

        public static string JoinString(this IEnumerable<string> source, string separator)
        {
            return String.Join(separator, source);
        }

        public static IEnumerable<T> TakeIfNotNull<T>(this IEnumerable<T> source, int? take)
        {
            return take == null ? source : source.Take(take.Value);
        }
    }
}
