using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using StarryEyes.Albireo.Data;
using StarryEyes.Casket.Querying;

namespace StarryEyes.Casket.Infrastructure
{
    public abstract class CachedStore<T> : IDisposable
    {
        private LinkedList<long> _deleteCaches = new LinkedList<long>();
        private AVLTree<long> _deleteTree = new AVLTree<long>();

        private readonly LinkedList<T> _aliveCaches = new LinkedList<T>();
        private readonly SortedDictionary<long, LinkedListNode<T>> _aliveCacheFinder =
            new SortedDictionary<long, LinkedListNode<T>>();
        private readonly object _aliveCachesLocker = new object();

        private readonly LinkedList<PersistentItem<T>> _deadlyCaches = new LinkedList<PersistentItem<T>>();
        private readonly SortedDictionary<long, LinkedListNode<PersistentItem<T>>> _deadlyCacheFinder =
            new SortedDictionary<long, LinkedListNode<PersistentItem<T>>>();
        private readonly object _deadlyCachesLocker = new object();

        protected abstract long KeyProvider(T obj);

        private readonly Thread _internalThread;

        private bool _disposed;

        public virtual int CacheSize
        {
            get { return 1024; }
        }

        public CachedStore()
        {
            _internalThread = new Thread(this.WritebackThread);
            _internalThread.Start();
        }

        public IObservable<T> Query(IQuery<T> query)
        {
        }

        public void Store(T item)
        {

        }

        public void Delete(long key)
        {

        }

        private void WritebackThread()
        {
            while (!_disposed)
            {
                lock (_internalThread)
                {
                    Monitor.Wait(_internalThread);
                }
            }
        }

        protected abstract Task<IEnumerable<T>> QueryDb(string sql, long? lowerBound, long? upperBound);

        protected abstract void Writeback(T item);

        protected void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }
        }

        public void Dispose()
        {
            _disposed = true;
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~CachedStore()
        {
            if (!_disposed)
            {
                this.Dispose(false);
            }
        }

        // ReSharper disable UnusedParameter.Global
        protected virtual void Dispose(bool disposing)
        {
        }
        // ReSharper restore UnusedParameter.Global
    }
}
