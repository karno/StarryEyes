using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using StarryEyes.Vanille.Serialization;

namespace StarryEyes.Vanille.DataStore.Persistent
{
    internal class PersistentChunk<TKey, TValue> : 
        IDisposable where TValue : IBinarySerializable, new()
    {
        const int aliveToDeadlyThreshold = 1024;
        const int deadlyToKillThreshold = 512;

        private PersistentDataStore<TKey, TValue> _parent;

        private LinkedList<TKey> deletedItems = new LinkedList<TKey>();
        private object deletedItemKeyLocker = new object();

        private LinkedList<TValue> aliveCaches = new LinkedList<TValue>();
        private object aliveCachesLocker = new object();

        private LinkedList<TValue> deadlyCaches = new LinkedList<TValue>();
        private object deadlyCachesLocker = new object();

        private ReaderWriterLockSlim driveLocker = new ReaderWriterLockSlim();
        private PersistentDrive<TKey, TValue> persistentDrive;
        private Thread writeBackWorker;
        private object writeBackSync = new object();
        private volatile bool writeBackThreadAlive = true;

        /// <summary>
        /// initialize persistent chunk.
        /// </summary>
        /// <param name="parent">chunk holder</param>
        /// <param name="dbFilePath">file path for storing data</param>
        public PersistentChunk(PersistentDataStore<TKey, TValue> parent, string dbFilePath)
        {
            this._parent = parent;
            using (AcquireDriveLock(true))
            {
                this.persistentDrive = new PersistentDrive<TKey, TValue>(dbFilePath);
            }
            this.writeBackWorker = new Thread(WriteBackProc);
            this.writeBackWorker.Start();
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
            this._parent = parent;
            using (AcquireDriveLock(true))
            {
                this.persistentDrive = new PersistentDrive<TKey, TValue>(dbFilePath, tableOfContents, nextIndexOfPackets);
            }
            this.writeBackWorker = new Thread(WriteBackProc);
            this.writeBackWorker.Start();
        }

        /// <summary>
        /// get amount of chunk items
        /// </summary>
        public int Count
        {
            get
            {
                int amount = 0;
                lock (aliveCachesLocker)
                {
                    amount += aliveCaches.Count;
                }
                lock (deadlyCachesLocker)
                {
                    amount += deadlyCaches.Count;
                }
                lock (deletedItemKeyLocker)
                {
                    amount -= deletedItems.Count;
                }
                lock (driveLocker)
                {
                    amount += persistentDrive.Count;
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
            TValue deadlyItem = default(TValue);
            lock (aliveCachesLocker)
            {
                // alive cache
                var node = FindNodeByKey(aliveCaches, key);
                if (node != null)
                {
                    node.Value = value; // replace previous
                    aliveCaches.Remove(node);
                    aliveCaches.AddFirst(node); // move node to top
                }
                else
                {
                    aliveCaches.AddFirst(value); // add top
                }
                if (aliveCaches.Count > aliveToDeadlyThreshold)
                {
                    overflow = true;
                    deadlyItem = aliveCaches.Last.Value;
                    aliveCaches.RemoveLast();
                }
            }

            lock (deletedItemKeyLocker)
            {
                // remove key from deleted store
                deletedItems.Remove(key);
            }

            if (overflow)
                AddToDeadly(_parent.GetKey(deadlyItem), deadlyItem);
        }

        /// <summary>
        /// add key-value to deadly cache.
        /// </summary>
        /// <param name="key">add key</param>
        /// <param name="value">add value</param>
        private void AddToDeadly(TKey key, TValue value)
        {
            bool writeBackRequired = false;
            lock (deadlyCachesLocker)
            {
                var dnode = FindNodeByKey(deadlyCaches, key);
                if (dnode != null)
                    dnode.Value = value;
                else
                    deadlyCaches.AddFirst(value);
                writeBackRequired = deadlyCaches.Count > deadlyToKillThreshold;
            }
            if (writeBackRequired)
            {
                lock (writeBackSync)
                {
                    Monitor.Pulse(writeBackSync);
                }
            }
        }

        /// <summary>
        /// Find linked list node by a key
        /// </summary>
        /// <param name="list">target list</param>
        /// <param name="key">search key</param>
        /// <returns>actual node or null reference</returns>
        private LinkedListNode<TValue> FindNodeByKey(LinkedList<TValue> list, TKey key)
        {
            if(list.First == null)
                return null;
            return EnumerableEx.Generate(
                list.First,
                node => node.Next != null,
                node => node.Next,
                node => node)
                .Where(i => _parent.GetKey(i.Value).Equals(key))
                .FirstOrDefault();
        }

        private void WriteBackProc()
        {
            while (true)
            {
                lock (writeBackSync)
                {
                    if (writeBackThreadAlive)
                        Monitor.Wait(writeBackSync);
                }
                if (!writeBackThreadAlive)
                    return;
                System.Diagnostics.Debug.WriteLine("write-backing...");
                List<LinkedListNode<TValue>> workingCopy = new List<LinkedListNode<TValue>>();
                lock (deadlyCachesLocker)
                {
                    EnumerableEx.Generate(
                        deadlyCaches.First,
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
                        .Select(n => n.Value)
                        .ForEach(v => persistentDrive.Store(_parent.GetKey(v), v));
                }
                Thread.Sleep(0);
                lock (deadlyCachesLocker)
                {
                    workingCopy.ForEach(n => deadlyCaches.Remove(n));
                }
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
                driveLocker.EnterWriteLock();
                return Disposable.Create(() => driveLocker.ExitWriteLock());
            }
            else
            {
                driveLocker.EnterReadLock();
                return Disposable.Create(() => driveLocker.ExitReadLock());
            }
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
            lock (deletedItemKeyLocker)
            {
                if (!deletedItems.Contains(key))
                    deletedItems.AddFirst(key);
            }
        }

        /// <summary>
        /// Get item from persistent chunk.
        /// </summary>
        public IObservable<TValue> Get(TKey key)
        {
            return Observable.Start(() =>
            {
                lock (deletedItemKeyLocker)
                {
                    if (deletedItems.Contains(key))
                        return Observable.Empty<TValue>();
                }
                lock (aliveCachesLocker)
                {
                    var node = FindNodeByKey(aliveCaches, key);
                    if (node != null)
                        return Observable.Return(node.Value);
                }
                lock (deadlyCachesLocker)
                {
                    var node = FindNodeByKey(deadlyCaches, key);
                    if (node != null)
                        return Observable.Return(node.Value);
                }
                // disk access
                using (AcquireDriveLock())
                {
                    try
                    {
                        return Observable.Return(persistentDrive.Load(key));
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
        /// <param name="predicate"></param>
        /// <returns></returns>
        public IObservable<TValue> Find(Func<TValue, bool> predicate)
        {
            return
                Observable.Start(() =>
                {
                    lock (aliveCachesLocker)
                    {
                        return aliveCaches.Where(v => predicate(v)).ToArray();
                    }
                })
                .Concat(Observable.Start(() =>
                {
                    lock (deadlyCachesLocker)
                    {
                        return deadlyCaches.Where(v => predicate(v)).ToArray();
                    }
                }))
                .Concat(Observable.Start<TValue[]>(() =>
                {
                    using (AcquireDriveLock())
                    {
                        return persistentDrive.Find(predicate).ToArray();
                    }
                }))
                .SelectMany(_ => _)
                .Distinct(v => _parent.GetKey(v));
        }

        /// <summary>
        /// Get table of contents dictionary
        /// </summary>
        public IDictionary<TKey, int> GetTableOfContents()
        {
            lock (driveLocker)
            {
                return new Dictionary<TKey, int>(persistentDrive.GetTableOfContents());
            }
        }

        public IEnumerable<int> GetNextIndexOfPacketsArray()
        {
            lock (driveLocker)
            {
                return persistentDrive.GetNextIndexOfPackets().ToArray();
            }
        }

        /// <summary>
        /// clean up all resources.
        /// </summary>
        public void Dispose()
        {
            lock (writeBackSync)
            {
                writeBackThreadAlive = false;
                Monitor.Pulse(writeBackSync);
            }
            System.Threading.Thread.Sleep(0);
            List<TValue> workingCopy = new List<TValue>();
            // write all data to persistent store
            lock (aliveCachesLocker)
            {
                workingCopy.AddRange(aliveCaches);
            }
            lock (deadlyCachesLocker)
            {
                workingCopy.AddRange(deadlyCaches);
            }
            using (AcquireDriveLock(true))
            {
                workingCopy
                    .ForEach(v => persistentDrive.Store(_parent.GetKey(v), v));
                persistentDrive.Dispose();
            }
        }
    }
}
