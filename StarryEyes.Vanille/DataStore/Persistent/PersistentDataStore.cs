using System;
using System.Linq;
using System.Reactive.Linq;
using StarryEyes.Vanille.Serialization;

namespace StarryEyes.Vanille.DataStore.Persistent
{
    /// <summary>
    /// provide automated serialization service.
    /// </summary>
    /// <typeparam name="TValue">serialization content type</typeparam>
    public sealed class PersistentDataStore<TKey, TValue> :
        DataStoreBase<TKey, TValue> where TValue : IBinarySerializable, new()
    {
        private PersistenceChunk<TKey, TValue>[] chunks = null;
        private int chunkNum;

        /// <summary>
        /// Number of chunks
        /// </summary>
        public int ChunkNum
        {
            get { return chunkNum; }
        }

        /// <summary>
        /// Initialize DynamicCache
        /// </summary>
        /// <param name="baseDirectoryPath">path for serialize objects</param>
        /// <param name="chunkNum">cache separate count</param>
        public PersistentDataStore(Func<TValue, TKey> keyProvider, string baseDirectoryPath, int chunkNum = 32)
            : base(keyProvider)
        {
            this.chunkNum = chunkNum;
            this.chunks = Enumerable.Range(0, chunkNum)
                .Select(_ => new PersistenceChunk<TKey, TValue>(this, baseDirectoryPath))
                .ToArray();
        }


        /// <summary>
        /// Add item or update item
        /// </summary>
        /// <param name="value">store item</param>
        public override void Store(TValue value)
        {
            var key = this.GetKey(value);
            Observable.Start(() => GetChunk(key).AddOrUpdate(key, value))
                .Subscribe();
        }

        public override IObservable<TValue> Get(TKey key)
        {
            return GetChunk(key).Get(key);
        }

        public override IObservable<TValue> Find(Func<TValue, bool> predicate)
        {
            return chunks.ToObservable()
                .SelectMany(c => c.Find(predicate));
        }

        public override void Remove(TKey key)
        {
            GetChunk(key).Remove(key);
        }

        private PersistenceChunk<TKey, TValue> GetChunk(TKey key)
        {
            return chunks[key.GetHashCode() % chunkNum];
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            chunks.ForEach(c => c.Dispose());
        }
    }
}
