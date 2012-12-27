using StarryEyes.Vanille.Serialization;
using System;

namespace StarryEyes.Vanille.DataStore
{
    public abstract class DataStoreBase<TKey, TValue> : IDisposable
        where TKey : IComparable<TKey>
        where TValue : IBinarySerializable, new()
    {
        private readonly Func<TValue, TKey> _keyProvider;
        private bool _disposed;

        /// <summary>
        /// initialize abstract data store.
        /// </summary>
        /// <param name="keyProvider"></param>
        protected DataStoreBase(Func<TValue, TKey> keyProvider)
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
        /// Get stored data count.
        /// </summary>
        public abstract int Count { get; }

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
        /// <param name="predicate">find predicate</param>
        /// <param name="range">find ID range</param>
        /// <param name="returnLowerBound">count of items, lower bound.</param>
        /// <returns>all stored data</returns>
        public abstract IObservable<TValue> Find(Func<TValue, bool> predicate, FindRange<TKey> range = null, int? returnLowerBound = null);

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
            if (_disposed) return;
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
