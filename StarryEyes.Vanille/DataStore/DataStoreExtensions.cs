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
            return collection.Where(v => range.IsIn(keyProvider(v)));
        }

        internal static IObservable<TValue> CheckRange<TKey, TValue>(this IObservable<TValue> collection,
            FindRange<TKey> range, Func<TValue, TKey> keyProvider) where TKey : IComparable<TKey>
        {
            if (range == null)
                return collection;
            return collection.Where(v => range.IsIn(keyProvider(v)));
        }
    }
}
