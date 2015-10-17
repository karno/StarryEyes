using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StarryEyes.Models.Databases.Caching
{
    public class TaskQueue<TKey, TValue>
    {
        private readonly int _threshold;
        private readonly object _intvlock = new object();
        private readonly Action<IEnumerable<TValue>> _writeback;
        private readonly TimeSpan _minInterval;

        private readonly Dictionary<TKey, LinkedListNode<TValue>> _table =
            new Dictionary<TKey, LinkedListNode<TValue>>();

        private readonly LinkedList<TValue> _queue = new LinkedList<TValue>();

        private DateTime _lastWritebackStamp;

        public TaskQueue(int threshold, TimeSpan minInterval, Action<IEnumerable<TValue>> writeback)
        {
            _threshold = threshold;
            _writeback = writeback;
            _minInterval = minInterval;
            _lastWritebackStamp = DateTime.Now;
        }

        public int Count
        {
            get
            {
                lock (_queue)
                {
                    return _queue.Count;
                }
            }
        }

        public void Enqueue(TKey key, TValue value)
        {
            int count;
            lock (_queue)
            {
                LinkedListNode<TValue> node;
                if (_table.TryGetValue(key, out node))
                {
                    _queue.Remove(node);
                }
                _table[key] = _queue.AddFirst(value);
                count = _queue.Count;
            }
            if (count <= _threshold) return;
            lock (_intvlock)
            {
                var span = DateTime.Now - _lastWritebackStamp;
                if (span < _minInterval) return;
                _lastWritebackStamp = DateTime.Now;
            }
            Task.Run(() => Writeback());
        }

        public bool Contains(TKey key)
        {
            lock (_queue)
            {
                return _table.ContainsKey(key);
            }
        }

        public bool TryGetValue(TKey id, out TValue value)
        {
            lock (_queue)
            {
                LinkedListNode<TValue> node;
                if (_table.TryGetValue(id, out node))
                {
                    value = node.Value;
                    return true;
                }
            }
            value = default(TValue);
            return false;
        }

        public void Remove(TKey id)
        {
            lock (_queue)
            {
                LinkedListNode<TValue> node;
                if (_table.TryGetValue(id, out node))
                {
                    _queue.Remove(node);
                    _table.Remove(id);
                }
            }
        }

        public IEnumerable<TValue> Find(Func<TValue, bool> predicate)
        {
            lock (_queue)
            {
                return _queue.Where(predicate).ToArray();
            }
        }

        public void Writeback()
        {
            TValue[] list;
            lock (_queue)
            {
                list = _queue.ToArray();
                _queue.Clear();
                _table.Clear();
            }
            _writeback(list);
        }

        public void Clear()
        {
            lock (_queue)
            {
                _queue.Clear();
                _table.Clear();
            }
        }
    }
}
