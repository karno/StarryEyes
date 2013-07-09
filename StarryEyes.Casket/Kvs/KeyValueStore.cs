using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StarryEyes.Casket.Serialization;

namespace StarryEyes.Casket.Kvs
{
    public class KeyValueStore<T> where T : IBinarySerializable
    {
        private readonly Func<T, long> _idProvider;
        private readonly int _pageSize;

        private LinkedList<TreePage<T>> _cache = new LinkedList<TreePage<T>>();
        private SortedDictionary<long, LinkedListNode<TreePage<T>>> _cacheLookup = new SortedDictionary<long, LinkedListNode<TreePage<T>>>();

        public KeyValueStore(string filePath, Func<T, long> idProvider, int pageSize)
        {
            _idProvider = idProvider;
            _pageSize = pageSize;
        }

        public async Task Store(T item)
        {
            var id = _idProvider(item);
        }

        public async Task<T> Get(long id)
        {
            throw new NotImplementedException();
        }

        public IObservable<T> Find(Func<T, bool> predicate, long? minId = null, long? maxId = null)
        {
            var min = minId ?? 0;
            var max = maxId ?? long.MaxValue;
            throw new NotImplementedException();
        }
    }
}
