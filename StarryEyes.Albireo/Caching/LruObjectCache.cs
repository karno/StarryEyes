using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;

namespace StarryEyes.Albireo.Caching
{
    public class LruObjectCache<TKey, TValue> : IDisposable where TValue : class
    {
        private const int CleanupInterval = 1000 * 60 * 3;

        private readonly ConcurrentDictionary<TKey, WeakReference<TValue>> _expiredPool =
            new ConcurrentDictionary<TKey, WeakReference<TValue>>();

        private readonly ConcurrentDictionary<TKey, LinkedListNode<KeyValuePair<TKey, TValue>>> _lookupTable =
            new ConcurrentDictionary<TKey, LinkedListNode<KeyValuePair<TKey, TValue>>>();

        private readonly LinkedList<KeyValuePair<TKey, TValue>> _cacheList =
        new LinkedList<KeyValuePair<TKey, TValue>>();

        private readonly int _cacheCount;

        private readonly Timer _cleanupTimer;

        public LruObjectCache(int cacheCount)
        {
            this._cacheCount = cacheCount;
            _cleanupTimer = new Timer(_ => CleanupExpiredCache(), null, 0, CleanupInterval);
        }

        public int Count
        {
            get { return this._lookupTable.Count; }
        }

        public bool IsReadOnly { get { return false; } }

        public void AddOrUpdate(TKey key, TValue value)
        {
            LinkedListNode<KeyValuePair<TKey, TValue>> node, newNode;
            if (_lookupTable.TryGetValue(key, out node))
            {
                // existed
                if (node.Value.Value.Equals(value))
                {
                    // already stored.
                    return;
                }
                lock (_cacheList)
                {
                    // remove previous node
                    _cacheList.Remove(node);
                    // add node
                    newNode = _cacheList.AddFirst(new KeyValuePair<TKey, TValue>(key, value));
                }
                // remove previous table
                if (!_lookupTable.TryUpdate(key, newNode, node))
                {
                    // conflicted -> rollback 
                    lock (_cacheList)
                    {
                        _cacheList.Remove(newNode);
                    }
                }
            }
        }

        public void Clear()
        {
            lock (_cacheList)
            {
                _cacheList.Clear();
                _lookupTable.Clear();
                _expiredPool.Clear();
            }
        }

        public bool Contains(TKey key)
        {
            return _lookupTable.ContainsKey(key) ||
                   _expiredPool.ContainsKey(key);
        }

        public void Remove(TKey key)
        {
            LinkedListNode<KeyValuePair<TKey, TValue>> item;
            if (_lookupTable.TryRemove(key, out item))
            {
                lock (_cacheList)
                {
                    _cacheList.Remove(item);
                }
            }
            else
            {
                WeakReference<TValue> wr;
                _expiredPool.TryRemove(key, out wr);
            }
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            // check alive cache
            LinkedListNode<KeyValuePair<TKey, TValue>> node;
            if (_lookupTable.TryGetValue(key, out node))
            {
                value = node.Value.Value;
                return true;
            }
            // check expired cache
            if (this.GetFromExpiredPool(key, out value))
            {
                AddOrUpdate(key, value);
                return true;
            }
            return false;
        }

        #region Clean up and resurrection

        public void CleanupExpiredCache()
        {
            // expire cache
            lock (_cacheList)
            {
                while (_cacheList.Count > _cacheCount)
                {
                    var last = _cacheList.Last.Value;
                    _cacheList.RemoveLast();
                    _expiredPool.TryAdd(last.Key, new WeakReference<TValue>(last.Value));
                }
            }
            GC.Collect(1, GCCollectionMode.Optimized);
            // erase collected object references
            var keys = _expiredPool.Keys.ToArray();
            foreach (var key in keys)
            {
                WeakReference<TValue> wr;
                TValue value;
                if (_expiredPool.TryGetValue(key, out wr) && !wr.TryGetTarget(out value))
                {
                    _expiredPool.TryRemove(key, out wr);
                }
            }
        }

        private bool GetFromExpiredPool(TKey key, out TValue cache)
        {
            cache = default(TValue);
            WeakReference<TValue> wr;
            return this._expiredPool.TryRemove(key, out wr) &&
                   wr.TryGetTarget(out cache);
        }

        #endregion

        public ICollection<TValue> CreateSnapshot()
        {
            lock (_cacheList)
            {
                return new Collection<TValue>(_cacheList.Select(c => c.Value).ToList());
            }
        }

        public void Dispose()
        {
            _cleanupTimer.Dispose();
        }
    }
}
