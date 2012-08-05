using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using StarryEyes.Vanille.Serialization;

namespace StarryEyes.Vanille.DataStore.Persistent
{
    internal class PersistenceChunk<TKey, TValue> : 
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

        private PersistentDrive<TKey, TValue> persistentDrive;
        private object driveLocker = new object();

        /// <summary>
        /// initialize persistent chunk.
        /// </summary>
        /// <param name="parent">chunk holder</param>
        /// <param name="baseDirectoryPath">base directory path for making db structure</param>
        public PersistenceChunk(PersistentDataStore<TKey, TValue> parent,string baseDirectoryPath)
        {
            this._parent = parent;
            lock (driveLocker)
            {
                this.persistentDrive = new PersistentDrive<TKey, TValue>(baseDirectoryPath);
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
            lock (deadlyCachesLocker)
            {
                var dnode = FindNodeByKey(deadlyCaches, key);
                if (dnode != null)
                    dnode.Value = value;
                else
                    deadlyCaches.AddFirst(value);
            }
            CheckWriteback();
        }

        /// <summary>
        /// Find linked list node by a key
        /// </summary>
        /// <param name="list">target list</param>
        /// <param name="key">search key</param>
        /// <returns>actual node or null reference</returns>
        private LinkedListNode<TValue> FindNodeByKey(LinkedList<TValue> list, TKey key)
        {
            return EnumerableEx.Generate(
                list.First,
                node => node.Next != null,
                node => node.Next,
                node => node)
                .Where(i => _parent.GetKey(i.Value).Equals(key))
                .FirstOrDefault();
        }

        /// <summary>
        /// check deadly cache length and write-back it to persistent drive.
        /// </summary>
        private void CheckWriteback()
        {
            lock (deadlyCachesLocker)
            {
                if (deadlyCaches.Count <= deadlyToKillThreshold)
                    return;
                // store them

                // TODO: Implementation
                throw new NotImplementedException();

                // clear deadly cache
                deadlyCaches.Clear();
            }
        }

        public void Remove(TKey key)
        {
            lock (deletedItemKeyLocker)
            {
                if (!deletedItems.Contains(key))
                    deletedItems.AddFirst(key);
            }
        }

        public IObservable<TValue> Get(TKey key)
        {
            return Observable.Start(() =>
            {
                lock (deletedItemKeyLocker)
                {
                    if (deletedItems.Contains(key))
                        return default(TValue);
                }
                lock (aliveCachesLocker)
                {
                    var node = FindNodeByKey(aliveCaches, key);
                    if (node != null)
                        return node.Value;
                }
                lock (deadlyCachesLocker)
                {
                    var node = FindNodeByKey(deadlyCaches, key);
                    if (node != null)
                        return node.Value;
                }
                // disk access
                throw new NotImplementedException();
            })
            .Where(v => v != null);
        }

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
                    lock (driveLocker)
                    {
                        throw new NotImplementedException();
                    }
                }))
                .SelectMany(_ => _)
                .Distinct(v => _parent.GetKey(v));
        }

        /// <summary>
        /// clean up all resources.
        /// </summary>
        public void Dispose()
        {
            lock (driveLocker)
            {
                persistentDrive.Dispose();
            }
        }
    }
}
