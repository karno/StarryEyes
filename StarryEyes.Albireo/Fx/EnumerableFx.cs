using System.Collections.Generic;
using JetBrains.Annotations;

// ReSharper disable CheckNamespace

namespace System.Linq
    // ReSharper restore CheckNamespace
{
    public static class EnumerableFx
    {
        public static bool SyncSet<T>(this HashSet<T> set, IEnumerable<T> enumerates)
        {
            return SyncSet(set, new HashSet<T>(enumerates));
        }

        public static bool SyncSet<T>(this HashSet<T> set, HashSet<T> other)
        {
            bool changed = false;
            foreach (var item in other)
            {
                if (set.Add(item))
                {
                    changed = true;
                }
            }
            if (set.Count != other.Count)
            {
                foreach (var item in set.ToArray())
                {
                    if (other.Remove(item)) continue;
                    set.Remove(item);
                    changed = true;
                }
            }
            return changed;
        }

        private static readonly Random _rng = new Random();

        public static IEnumerable<T> Append<T>([NotNull] this IEnumerable<T> source, [CanBeNull] params T[] item)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return item == null ? source : source.Concat(item);
        }

        public static IOrderedEnumerable<TSource> Shuffle<TSource>([CanBeNull] this IEnumerable<TSource> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return source.OrderBy(_ => _rng.Next());
        }

        public static IEnumerable<T> Guard<T>([CanBeNull] this IEnumerable<T> source)
        {
            return source ?? Enumerable.Empty<T>();
        }

        public static IEnumerable<T> Guard<T>([CanBeNull] this IEnumerable<T> source, [CanBeNull] Func<bool> guard)
        {
            return guard() ? source.Guard() : Enumerable.Empty<T>();
        }

        public static IEnumerable<T> Guard<T>([CanBeNull] this IEnumerable<T> source,
            [CanBeNull] Func<IEnumerable<T>, bool> guard)
        {
            var buffer = source.Guard().Memoize();
            return guard(buffer) ? buffer : Enumerable.Empty<T>();
        }

        public static TResult Using<T, TResult>(this T disposable, [NotNull] Func<T, TResult> func)
            where T : IDisposable
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            using (disposable) return func(disposable);
        }

        public static string JoinString([NotNull] this IEnumerable<string> source, [CanBeNull] string separator)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return String.Join(separator, source);
        }

        public static IEnumerable<T> TakeIfNotNull<T>([NotNull] this IEnumerable<T> source, int? take)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return take == null ? source : source.Take(take.Value);
        }
    }
}