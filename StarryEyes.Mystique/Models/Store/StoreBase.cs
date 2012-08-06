using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Reactive.Linq;
using System.Linq.Expressions;
using StarryEyes.Albireo.Data;
using System.Threading.Tasks;
using System.Data.Objects.DataClasses;

namespace StarryEyes.Mystique.Models.Store
{
    internal abstract class StoreBase<T, TDatabase>
        where T : class
        where TDatabase : EntityObject
    {
        private static object databaseLocker = new object();
        private static LocalDatabase database;
        protected static LocalDatabase Database
        {
            get { return StoreBase<T, TDatabase>.database; }
        }

        private object cacheTreeLocker = new object();
        private SortedDictionary<long, LinkedListNode<T>> cacheTree = new SortedDictionary<long, LinkedListNode<T>>();

        private object cacheLocker = new object();
        private LinkedList<T> cache = new LinkedList<T>();

        private object deletedsLocker = new object();
        private AVLTree<long> deleteds = new AVLTree<long>();

        private object writebackSync = new object();
        private Thread writebackThread = null;
        private bool writebackThreadAlive = true;

        const int CacheCleaningThreshold = 1024;
        const int CacheCleaningSurvivors = 512;

        private static event Action OnShutdownCallback;

        static StoreBase()
        {
            lock (databaseLocker)
            {
                database = new LocalDatabase();
            }
        }

        public StoreBase()
        {
            OnShutdownCallback += OnShutdown;
            lock (writebackSync)
            {
                writebackThread = new Thread(WriteBackProc);
                writebackThread.Start();
            }
        }

        /// <summary>
        /// Store item asynchronously.
        /// </summary>
        /// <param name="id">Item ID</param>
        /// <param name="item">Item Value</param>
        public void Store(long id, T item)
        {
            Observable.Start(() =>
            {
                LinkedListNode<T> leaf = null;
                lock (deletedsLocker)
                {
                    // remove item from deleted items list
                    deleteds.Remove(id);
                }
                lock (cacheTreeLocker)
                {
                    if (cacheTree.TryGetValue(id, out leaf))
                    {
                        leaf.Value = item;
                    }
                    else
                    {
                        leaf = new LinkedListNode<T>(item);
                        cacheTree.Add(id, leaf);
                    }
                }
                Task.Factory.StartNew(() =>
                {
                    bool requireCleaning = false;
                    lock (cacheLocker)
                    {
                        if (leaf.List != null)
                            cache.Remove(leaf);
                        cache.AddFirst(leaf);
                        requireCleaning = cache.Count > CacheCleaningThreshold;
                    }
                    if (requireCleaning)
                    {
                        lock (writebackSync)
                        {
                            Monitor.Pulse(writebackSync);
                        }
                    }
                });
            });
        }

        /// <summary>
        /// Write-back controller
        /// </summary>
        private void WriteBackProc()
        {
            while (writebackThreadAlive)
            {
                lock (writebackSync)
                {
                    Monitor.Wait(writebackSync);
                }
                List<LinkedListNode<T>> workingCopy = null;
                lock (cacheLocker)
                {
                    // copy deadly caches
                    if (cache.Count > CacheCleaningThreshold)
                    {
                        workingCopy = new List<LinkedListNode<T>>(cache.Count - CacheCleaningSurvivors);
                        for (var item = cache.Last;
                            workingCopy.Count < cache.Count - CacheCleaningSurvivors && item.Previous != null;
                            item = item.Previous)
                        {
                            workingCopy.Add(item);
                        }
                    }
                }

                List<long> deletedsCopy;
                lock (deletedsLocker)
                {
                    // copy deleteds
                    deletedsCopy = new List<long>(deleteds);
                }

                // remove deleted items from working copy
                // this compute free from locks.
                workingCopy = workingCopy
                    .Where(ln => !deletedsCopy.Contains(KeyProvider(ln.Value)))
                    .ToList();

                // write to DB
                lock (databaseLocker)
                {
                    foreach (var copy in workingCopy)
                    {
                        WriteItem(copy.Value);
                    }
                    database.SaveChanges();
                }

                // remove from cache
                lock (cacheLocker)
                {
                    workingCopy.ForEach(n => cache.Remove(n));
                }

                // remove from tree cache
                var ids = workingCopy.Select(n => this.KeyProvider(n.Value)).ToArray();
                lock (cacheTreeLocker)
                {
                    ids.ForEach(id => cacheTree.Remove(id));
                }

                // remove from deleteds list
                lock (deletedsLocker)
                {
                    deletedsCopy.ForEach(id => deleteds.Remove(id));
                }
            }
        }

        /// <summary>
        /// Get item from ID asynchronously.
        /// </summary>
        /// <param name="id">Finding item's id</param>
        /// <returns>Item entity or empty observable</returns>
        public IObservable<T> Get(long id)
        {
            return Observable.Start(() =>
            {
                LinkedListNode<T> result;
                lock (deletedsLocker)
                {
                    if (deleteds.Contains(id))
                        return Observable.Empty<T>();
                }
                // access to cache
                lock (cacheTreeLocker)
                {
                    if (cacheTree.TryGetValue(id, out result))
                    {
                        return Observable.Return(result.Value);
                    }
                }
                // access to DB
                var item = FindItem(id);
                // write-back to the cache
                if (item != null)
                {
                    Store(id, item);
                    return Observable.Return(item);
                }
                else
                {
                    return Observable.Empty<T>();
                }
            }).SelectMany(_ => _);
        }

        /// <summary>
        /// Filter item with predicate asynchronously.
        /// </summary>
        /// <param name="predicate">Finding item's constraint</param>
        /// <returns>items set observable</returns>
        public IObservable<T> Find(Func<T, bool> predicate, Expression<Func<TDatabase, bool>> dbPredicate)
        {
            return Observable.Start(() =>
                {
                    lock (cacheLocker)
                    {
                        return cache
                            .Where(predicate)
                            .ToList()
                            .AsEnumerable();
                    }
                })
                .SelectMany(_ => _)
                .Where(t =>
                {
                    lock (deletedsLocker)
                    {
                        return !deleteds.Contains(KeyProvider(t));
                    }
                })
                .Concat(Observable.Start(() => FindItems(dbPredicate)).SelectMany(_ => _))
                .Distinct(KeyProvider);
        }

        /// <summary>
        /// register as removed items.
        /// </summary>
        /// <param name="id"></param>
        public void Remove(long id)
        {
            Observable.Start(() =>
            {
                lock (deletedsLocker)
                {
                    deleteds.Add(id);
                }
            });
        }

        protected abstract long KeyProvider(T item);

        protected abstract void WriteItem(T item);

        protected abstract T FindItem(long id);

        protected abstract IEnumerable<T> FindItems(Expression<Func<TDatabase, bool>> predicate);

        protected abstract void DeleteItem(long key);

        public void Shutdown()
        {
            OnShutdown();
        }

        public virtual void OnShutdown()
        {
            OnShutdownCallback -= OnShutdown;
        }

        public static void GlobalShutdown()
        {
            OnShutdownCallback();
            database.Dispose();
        }
    }
}
