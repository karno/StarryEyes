using System;
using StarryEyes.Vanille.Serialization;

namespace StarryEyes.Vanille.DataStore
{
    public abstract class DataStoreBase<TKey, TValue>
        : IDisposable where TValue : IBinarySerializable, new()
    {
        private Func<TValue, TKey> _keyProvider = null;
        private bool _disposed;

        /// <summary>
        /// initialize abstract data store.
        /// </summary>
        /// <param name="keyProvider"></param>
        public DataStoreBase(Func<TValue, TKey> keyProvider)
        {
            this._keyProvider = keyProvider;
        }

        /// <summary>
        /// get a key for a (stored) data.
        /// </summary>
        /// <param name="value">data</param>
        /// <returns>key</returns>
        public TKey GetKey(TValue value)
        {
            return _keyProvider(value);
        }

        /// <summary>
        /// store a value.
        /// </summary>
        /// <param name="value">storing value</param>
        public abstract void Store(TValue value);

        /// <summary>
        /// get stored data.
        /// </summary>
        /// <param name="key">key for the value</param>
        /// <returns>stored data</returns>
        public abstract IObservable<TValue> Get(TKey key);

        /// <summary>
        /// get all stored data.<para />
        /// this method may very slow.
        /// </summary>
        /// <returns>all stored data</returns>
        public abstract IObservable<TValue> Find(Func<TValue, bool> predicate);

        /// <summary>
        /// remove stored data from storage.
        /// </summary>
        /// <param name="key">removing data key</param>
        public abstract void Remove(TKey key);

        /// <summary>
        /// check this object is disposed.
        /// </summary>
        protected void CheckDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(this.GetType().Name);
        }

        /// <summary>
        /// clean up all resources.
        /// </summary>
        public void Dispose()
        {
            CheckDisposed();
            _disposed = true;
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~DataStoreBase()
        {
            if (_disposed) return;
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing) { }
    }
}
