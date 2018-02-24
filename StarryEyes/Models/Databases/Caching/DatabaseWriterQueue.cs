using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace StarryEyes.Models.Databases.Caching
{
    public class DatabaseWriterQueue<TKey, TValue> : IDisposable
    {
        private readonly int _threshold;
        private readonly TimeSpan _minInterval;
        private readonly Func<IEnumerable<TValue>, Task> _writeback;
        private readonly Task _writerThread;

        private readonly BlockingCollection<KeyValuePair<TKey, TValue>> _collection =
            new BlockingCollection<KeyValuePair<TKey, TValue>>();

        private readonly ConcurrentDictionary<TKey, TValue> _writeDictionary = new ConcurrentDictionary<TKey, TValue>();

        public DatabaseWriterQueue(int threshold, TimeSpan minInterval,
            [CanBeNull] Func<IEnumerable<TValue>, Task> writeback)
        {
            _threshold = threshold;
            _minInterval = minInterval;
            _writeback = writeback ?? throw new ArgumentNullException(nameof(writeback));
            _writerThread = Task.Factory.StartNew(WritebackWorker, CancellationToken.None,
                TaskCreationOptions.DenyChildAttach | TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }

        public int Count => _writeDictionary.Count;

        public void Enqueue(TKey key, TValue value)
        {
            var addItem = true;
            _writeDictionary.AddOrUpdate(key, value,
                (_, oldValue) =>
                {
                    if (oldValue.Equals(value))
                    {
                        addItem = false;
                        return oldValue;
                    }
                    return value;
                });
            if (addItem && !_collection.IsAddingCompleted)
            {
                try
                {
                    _collection.Add(new KeyValuePair<TKey, TValue>(key, value));
                }
                catch (InvalidOperationException)
                {
                } // adding completed
            }
        }

        public bool Contains(TKey key)
        {
            return _writeDictionary.ContainsKey(key);
        }

        public bool TryGetValue(TKey id, out TValue value)
        {
            return _writeDictionary.TryGetValue(id, out value);
        }

        public void Remove(TKey id)
        {
            TValue removal;
            _writeDictionary.TryRemove(id, out removal);
        }

        public IEnumerable<TValue> Find(Func<TValue, bool> predicate)
        {
            return _writeDictionary.Values
                                   .Where(item => item != null && predicate(item))
                                   .ToArray();
        }

        private async void WritebackWorker()
        {
            var stamp = DateTime.Now;
            var items = new List<KeyValuePair<TKey, TValue>>();
            try
            {
                foreach (var pair in _collection.GetConsumingEnumerable())
                {
                    TValue dvalue;
                    if (!_writeDictionary.TryGetValue(pair.Key, out dvalue) || !pair.Value.Equals(dvalue))
                    {
                        // element has been deleted or changed.
                        continue;
                    }
                    items.Add(pair);
                    if (items.Count <= _threshold || (DateTime.Now - stamp) < _minInterval) continue;
                    await _writeback(items.Select(p => p.Value)).ConfigureAwait(false);
                    foreach (var item in items)
                    {
                        _writeDictionary.TryRemove(item.Key, out dvalue);
                        // check item has changed
                        if (!item.Value.Equals(dvalue))
                        {
                            _writeDictionary.GetOrAdd(item.Key, dvalue);
                        }
                    }
                    // finalize
                    items.Clear();
                    stamp = DateTime.Now;
                }
            }
            catch (OperationCanceledException)
            {
                // disposed
            } // ensure all items has stored before leaving process loop
            if (items.Count > 0)
            {
                await _writeback(items.Select(p => p.Value)).ConfigureAwait(false);
            }
            // complete adding
            _collection.Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~DatabaseWriterQueue()
        {
            Dispose(false);
        }

        protected void Dispose(bool disposing)
        {
            _collection.CompleteAdding();
        }
    }
}