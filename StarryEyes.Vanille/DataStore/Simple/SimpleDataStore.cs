using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using StarryEyes.Vanille.Serialization;
using System.Reactive.Linq;

namespace StarryEyes.Vanille.DataStore.Simple
{
    /// <summary>
    /// Simple data store implemented AbstractDataStore.
    /// </summary>
    /// <typeparam name="TKey">Key of value</typeparam>
    /// <typeparam name="TValue">actual store item</typeparam>
    public class SimpleDataStore<TKey, TValue>
        : DataStoreBase<TKey, TValue> where TValue : IBinarySerializable, new()
    {
        private object dictLock = new object();
        private Dictionary<TKey, TValue> dictionary = new Dictionary<TKey, TValue>();

        public SimpleDataStore(Func<TValue, TKey> keyProvider) : base(keyProvider) { }

        public override void Store(TValue value)
        {
            lock (dictLock)
            {
                dictionary[GetKey(value)] = value;
            }
        }

        public override IObservable<TValue> Get(TKey key)
        {
            return Observable.Start(() =>
            {
                lock (dictLock)
                {
                    TValue value;
                    if (dictionary.TryGetValue(key, out value))
                        return Observable.Return(value);
                    else
                        return Observable.Empty<TValue>();
                }
            })
            .SelectMany(_ => _);
        }

        public override IObservable<TValue> Find(Func<TValue, bool> predicate)
        {
            return Observable.Start(() =>
            {
                lock (dictLock)
                {
                    return dictionary.Values.Where(predicate).ToArray();
                }
            })
            .SelectMany(_ => _);
        }

        public override void Remove(TKey key)
        {
            lock (dictLock)
            {
                dictionary.Remove(key);
            }
        }
    }
}
