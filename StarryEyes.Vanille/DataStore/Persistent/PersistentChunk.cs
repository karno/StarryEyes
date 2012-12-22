using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using StarryEyes.Vanille.Serialization;

namespace StarryEyes.Vanille.DataStore.Persistent
{
    internal class PersistentChunk<TKey, TValue> : IDisposable
        where TKey : IComparable<TKey>
        where TValue : IBinarySerializable, new()
    {
        const int AliveToDeadlyThreshold = 64;
        const int DeadlyToKillThreshold = 32;

        private readonly PersistentDataStore<TKey, TValue> _parent;

        private readonly LinkedList<TKey> _deletedItems = new LinkedList<TKey>();
        private readonly object _deletedItemKeyLocker = new object();

        private readonly LinkedList<TValue> _aliveCaches = new LinkedList<TValue>();
        private readonly SortedDictionary<TKey, LinkedListNode<TValue>> _aliveCacheFinder =
            new SortedDictionary<TKey, LinkedListNode<TValue>>();
        private readonly object _aliveCachesLocker = new object();

        private readonly LinkedList<PersistentItem<TValue>> _deadlyCaches = new LinkedList<PersistentItem<TValue>>();
        private readonly SortedDictionary<TKey, LinkedListNode<PersistentItem<TValue>>> _deadlyCacheFinder =
            new SortedDictionary<TKey, LinkedListNode<PersistentItem<TValue>>>();
        private readonly object _deadlyCachesLocker = new object();

        private readonly ReaderWriterLockSlim _driveLocker = new ReaderWriterLockSlim();
        private readonly PersistentDrive<TKey, TValue> _persistentDrive;
        private readonly Thread _writeBackWorker;
        private readonly object _writeBackSync = new object();
        private volatile bool _writeBackThreadAlive = true;

        /// <summary>
        /// initialize persistent chunk.
        /// </summary>
        /// <param name="parent">chunk holder</param>
        /// <param name="dbFilePath">file path for storing data</param>
        /// <param name="comparer">comparator using sort key.</param>
        public PersistentChunk(PersistentDataStore<TKey, TValue> parent, string dbFilePath, IComparer<TKey> comparer)
        {
            _parent = parent;
            using (AcquireDriveLock(true))
            {
                _persistentDrive = new PersistentDrive<TKey, TValue>(dbFilePath, comparer);
            }
            _writeBackWorker = new Thread(WriteBackProc);
            _writeBackWorker.Start();
        }

        /// <summary>
        /// Initialize persistent chunk with previous data.
        /// </summary>
        /// <param name="parent">chunk holder</param>
        /// <param name="dbFilePath">file path for storing data</param>
        /// <param name="tableOfContents"></param>
        /// <param name="nextIndexOfPackets"></param>
        public PersistentChunk(PersistentDataStore<TKey, TValue> parent, string dbFilePath,
            IDictionary<TKey, int> tableOfContents, IEnumerable<int> nextIndexOfPackets)
        {
            _parent = parent;
            using (AcquireDriveLock(true))
            {
                _persistentDrive = new PersistentDrive<TKey, TValue>(dbFilePath, tableOfContents, nextIndexOfPackets);
            }
            Task.Factory.StartNew(WriteBackProc, TaskCreationOptions.LongRunning);
        }

        /// <summary>
        /// get amount of chunk items
        /// </summary>
        public int Count
        {
            get
            {
                int amount = 0;
                lock (_aliveCachesLocker)
                {
                    amount += _aliveCaches.Count;
                }
                lock (_deadlyCachesLocker)
                {
                    amount += _deadlyCaches.Count;
                }
                lock (_deletedItemKeyLocker)
                {
                    amount -= _deletedItems.Count;
                }
                lock (_driveLocker)
                {
                    amount += _persistentDrive.Count;
                }
                return amount;
            }
        }

        /// <summary>
        /// add or update cache item.
        /// </summary>
        /// <param name="key">add key</param>
        /// <param name="value">add value</param>
        public void AddOrUpdate(TKey key, TValue value)
        {
            AddToAlive(key, value);
        }

        /// <summary>
        /// add key-value to alive cache.
        /// </summary>
        /// <param name="key">add key</param>
        /// <param name="value">add value</param>
        private void AddToAlive(TKey key, TValue value)
        {
            bool overflow = false;
            TKey deadlyKey = default(TKey);
            TValue deadlyItem = default(TValue);
            lock (_aliveCachesLocker)
            {
                // alive cache
                LinkedListNode<TValue> node;
                if (_aliveCacheFinder.TryGetValue(key, out node))
                {
                    node.Value = value; // replace previous
                    _aliveCaches.Remove(node);
                    _aliveCaches.AddFirst(node); // move node to top
                }
                else
                {
                    node = new LinkedListNode<TValue>(value);
                    _aliveCaches.AddFirst(node);
                    _aliveCacheFinder.Add(key, node);
                }
                if (_aliveCaches.Count > AliveToDeadlyThreshold)
                {
                    overflow = true;
                    deadlyItem = _aliveCaches.Last.Value;
                    deadlyKey = _parent.GetKey(deadlyItem);
                    _aliveCacheFinder.Remove(deadlyKey);
                    _aliveCaches.RemoveLast();
                }
            }

            lock (_deadlyCachesLocker)
            {
                LinkedListNode<PersistentItem<TValue>> nitem;
                if (_deadlyCacheFinder.TryGetValue(key, out nitem))
                {
                    // alive caches added.
                    // remove from deadly
                    _deadlyCacheFinder.Remove(key);
                    _deadlyCaches.Remove(nitem);
                }
            }

            lock (_deletedItemKeyLocker)
            {
                // remove key from deleted store
                _deletedItems.Remove(key);
            }

            if (overflow)
                AddToDeadly(deadlyKey, deadlyItem);
        }

        /// <summary>
        /// add key-value to deadly cache.
        /// </summary>
        /// <param name="key">add key</param>
        /// <param name="value">add value</param>
        private void AddToDeadly(TKey key, TValue value)
        {
            bool writeBackRequired;
            lock (_deadlyCachesLocker)
            {
                LinkedListNode<PersistentItem<TValue>> dnode;
                if (_deadlyCacheFinder.TryGetValue(key, out dnode))
                {
                    dnode.Value.Item = value;
                }
                else
                {
                    dnode = new LinkedListNode<PersistentItem<TValue>>(
                        new PersistentItem<TValue>(value));
                    _deadlyCaches.AddFirst(dnode);
                    _deadlyCacheFinder.Add(key, dnode);
                }
                writeBackRequired = _deadlyCaches.Count > DeadlyToKillThreshold;
            }
            if (writeBackRequired)
            {
                lock (_writeBackSync)
                {
                    Monitor.Pulse(_writeBackSync);
                }
            }
        }

        private void WriteBackProc()
        {
            var workingCopy = new List<LinkedListNode<PersistentItem<TValue>>>();
            while (true)
            {
                lock (_writeBackSync)
                {
                    if (_writeBackThreadAlive)
                        Monitor.Wait(_writeBackSync);
                }
                if (!_writeBackThreadAlive)
                    return;
                System.Diagnostics.Debug.WriteLine("write-backing...");
                lock (_deadlyCachesLocker)
                {
                    EnumerableEx.Generate(
                        _deadlyCaches.First,
                        node => node.Next != null,
                        node => node.Next,
                        node => node)
                        .ForEach(workingCopy.Add);
                }
                Thread.Sleep(0);
                System.Diagnostics.Debug.WriteLine("write-back " + workingCopy.Count + " objects.");
                using (AcquireDriveLock(true))
                {
                    workingCopy
                        .Do(n => n.Value.WriteFlag = true)
                        .Select(n => n.Value.Item)
                        .ForEach(v => _persistentDrive.Store(_parent.GetKey(v), v));
                }
                Thread.Sleep(0);
                lock (_deadlyCachesLocker)
                {
                    workingCopy
                        .Where(i => i.Value.WriteFlag)
                        .ForEach(n =>
                        {
                            if (n.List != null)
                                _deadlyCaches.Remove(n);
                            _deadlyCacheFinder.Remove(_parent.GetKey(n.Value.Item));
                        });
                }
                // release memory
                workingCopy.Clear();

                GC.Collect(); // collect garbage.
                Thread.Sleep(0);
            }
        }

        /// <summary>
        /// Acquire read/write lock for deadly cache.
        /// </summary>
        /// <param name="writeLock"></param>
        /// <returns></returns>
        private IDisposable AcquireDriveLock(bool writeLock = false)
        {
            if (writeLock)
            {
                _driveLocker.EnterWriteLock();
                return Disposable.Create(() => _driveLocker.ExitWriteLock());
            }
            _driveLocker.EnterReadLock();
            return Disposable.Create(() => _driveLocker.ExitReadLock());
        }

        /// <summary>
        /// remove item from persistent chunk
        /// </summary>
        /// <param name="key">delete item key</param>
        public void Remove(TKey key)
        {
            /*
             * DELETE STRATEGY:
             * 1: add DeletedItems collection
             * 2: when write-backing to persistent drive, deleted items are also write-back into it.
             */
            lock (_deletedItemKeyLocker)
            {
                if (!_deletedItems.Contains(key))
                    _deletedItems.AddFirst(key);
            }
        }

        /// <summary>
        /// Get item from persistent chunk.
        /// </summary>
        public IObservable<TValue> Get(TKey key)
        {
            return Observable.Start(() =>
            {
                lock (_deletedItemKeyLocker)
                {
                    if (_deletedItems.Contains(key))
                        return Observable.Empty<TValue>();
                }
                lock (_aliveCachesLocker)
                {
                    LinkedListNode<TValue> node;
                    if (_aliveCacheFinder.TryGetValue(key, out node))
                        return Observable.Return(node.Value);
                }
                lock (_deadlyCachesLocker)
                {
                    LinkedListNode<PersistentItem<TValue>> node;
                    if (_deadlyCacheFinder.TryGetValue(key, out node))
                        return Observable.Return(node.Value.Item);
                }
                // disk access
                using (AcquireDriveLock())
                {
                    try
                    {
                        return Observable.Return(_persistentDrive.Load(key));
                    }
                    catch
                    {
                        return Observable.Empty<TValue>();
                    }
                }
            })
            .SelectMany(_ => _);
        }

        /// <summary>
        /// Find item with predicate
        /// </summary>
        /// <param name="predicate">predicate for finding items.</param>
        /// <param name="range">range value</param>
        /// <param name="returnLowerBound">lower bound count of returning items.</param>
        /// <returns></returns>
        public IObservable<TValue> Find(Func<TValue, bool> predicate, FindRange<TKey> range, int? returnLowerBound)
        {
            return
                Observable.Defer(() => Observable.Merge(
                    Observable.Start(() =>
                    {
                        lock (_aliveCachesLocker)
                        {
                            return _aliveCaches
                                .CheckRange(range, _parent.GetKey)
                                .Where(predicate)
                                .ToArray();
                        }

                    }),
                    Observable.Start(() =>
                    {
                        lock (_deadlyCachesLocker)
                        {
                            return _deadlyCaches
                                .Select(v => v.Item)
                                .CheckRange(range, _parent.GetKey)
                                .Where(predicate)
                                .ToArray();
                        }
                    }),
                    Observable.Start(() =>
                    {
                        using (AcquireDriveLock())
                        {
                            return _persistentDrive
                                .Find(predicate, range, returnLowerBound)
                                .ToArray();
                        }
                    })))
                .SelectMany(_ => _)
                .Distinct(v => _parent.GetKey(v));
        }

        /// <summary>
        /// Get table of contents dictionary
        /// </summary>
        public IDictionary<TKey, int> GetTableOfContents()
        {
            lock (_driveLocker)
            {
                return new Dictionary<TKey, int>(_persistentDrive.GetTableOfContents());
            }
        }

        public IEnumerable<int> GetNextIndexOfPacketsArray()
        {
            lock (_driveLocker)
            {
                return _persistentDrive.GetNextIndexOfPackets().ToArray();
            }
        }

        /// <summary>
        /// clean up all resources.
        /// </summary>
        public void Dispose()
        {
            lock (_writeBackSync)
            {
                _writeBackThreadAlive = false;
                Monitor.Pulse(_writeBackSync);
            }
            Thread.Sleep(0);
            var workingCopy = new List<TValue>();
            // write all data to persistent store
            lock (_aliveCachesLocker)
            {
                workingCopy.AddRange(_aliveCaches);
            }
            lock (_deadlyCachesLocker)
            {
                workingCopy.AddRange(_deadlyCaches.Select(i => i.Item));
            }
            using (AcquireDriveLock(true))
            {
                workingCopy
                    .ForEach(v => _persistentDrive.Store(_parent.GetKey(v), v));
                _persistentDrive.Dispose();
            }
        }
    }
}
