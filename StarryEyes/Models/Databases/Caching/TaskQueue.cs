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
            this._threshold = threshold;
            this._writeback = writeback;
            this._minInterval = minInterval;
            this._lastWritebackStamp = DateTime.Now;
        }

        public int Count
        {
            get
            {
                lock (this._queue)
                {
                    return _queue.Count;
                }
            }
        }

        public void Enqueue(TKey key, TValue value)
        {
            int count;
            lock (this._queue)
            {
                LinkedListNode<TValue> node;
                if (this._table.TryGetValue(key, out node))
                {
                    this._queue.Remove(node);
                }
                this._table[key] = this._queue.AddFirst(value);
                count = this._queue.Count;
            }
            if (count <= this._threshold) return;
            lock (this._intvlock)
            {
                var span = DateTime.Now - this._lastWritebackStamp;
                if (span < this._minInterval) return;
                this._lastWritebackStamp = DateTime.Now;
            }
            Task.Run(() => this.Writeback());
        }

        public bool Contains(TKey key)
        {
            lock (this._queue)
            {
                return this._table.ContainsKey(key);
            }
        }

        public bool TryGetValue(TKey id, out TValue value)
        {
            lock (this._queue)
            {
                LinkedListNode<TValue> node;
                if (this._table.TryGetValue(id, out node))
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
            lock (this._queue)
            {
                LinkedListNode<TValue> node;
                if (this._table.TryGetValue(id, out node))
                {
                    this._queue.Remove(node);
                    this._table.Remove(id);
                }
            }
        }

        public IEnumerable<TValue> Find(Func<TValue, bool> predicate)
        {
            lock (this._queue)
            {
                return this._queue.Where(predicate).ToArray();
            }
        }

        public void Writeback()
        {
            TValue[] list;
            lock (this._queue)
            {
                list = this._queue.ToArray();
                this._queue.Clear();
                this._table.Clear();
            }
            this._writeback(list);
        }

        public void Clear()
        {
            lock (this._queue)
            {
                this._queue.Clear();
                this._table.Clear();
            }
        }
    }
}
