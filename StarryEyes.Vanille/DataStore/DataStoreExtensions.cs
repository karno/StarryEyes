using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace StarryEyes.Vanille.DataStore
{
    internal static class DataStoreExtensions
    {
        internal static IEnumerable<TValue> CheckRange<TKey, TValue>(this IEnumerable<TValue> collection,
            FindRange<TKey> range, Func<TValue, TKey> keyProvider) where TKey : IComparable<TKey>
        {
            if (range == null)
                return collection;
            else
                return collection.Where(v => range.IsIn(keyProvider(v)));
        }

        internal static IObservable<TValue> CheckRange<TKey, TValue>(this IObservable<TValue> collection,
            FindRange<TKey> range, Func<TValue, TKey> keyProvider) where TKey : IComparable<TKey>
        {
            if (range == null)
                return collection;
            else
                return collection.Where(v => range.IsIn(keyProvider(v)));
        }

        internal static IEnumerable<T> Take2<T>(this IEnumerable<T> collection, int? count)
        {
            if (count == null)
                return collection;
            else
                return collection.Take(count.Value);
        }

        internal static IObservable<T> Take2<T>(this IObservable<T> collection, int? count)
        {
            if (count == null)
                return collection;
            else
                return collection.Take(count.Value);
        }
    }
}
