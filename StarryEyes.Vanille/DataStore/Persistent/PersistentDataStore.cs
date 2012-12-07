using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using StarryEyes.Vanille.Serialization;

namespace StarryEyes.Vanille.DataStore.Persistent
{
    /// <summary>
    /// provide automated serialization service.
    /// </summary>
    /// <typeparam name="TValue">serialization content type</typeparam>
    public sealed class PersistentDataStore<TKey, TValue> : DataStoreBase<TKey, TValue>
        where TKey : IComparable<TKey>
        where TValue : IBinarySerializable, new()
    {
        private PersistentChunk<TKey, TValue>[] chunks = null;
        private int chunkNum;

        /// <summary>
        /// Number of chunks
        /// </summary>
        public int ChunkNum
        {
            get { return chunkNum; }
        }

        /// <summary>
        /// Initialize Persistent Data Store
        /// </summary>
        /// <param name="keyProvider">key provider for the value</param>
        /// <param name="baseDirectoryPath">path for serialize objects</param>
        /// <param name="chunkNum">cache separate count</param>
        /// <param name="tocniops">ToC/NIoPs</param>
        public PersistentDataStore(Func<TValue, TKey> keyProvider, string baseDirectoryPath,
            IComparer<TKey> comparer = null,
            int chunkNum = 32,
            IEnumerable<Tuple<IDictionary<TKey, int>, IEnumerable<int>>> tocniops = null)
            : base(keyProvider)
        {
            this.chunkNum = chunkNum;
            EnsurePath(baseDirectoryPath);
            if (tocniops != null)
            {
                var tna = tocniops.ToArray();
                if (tna.Length != chunkNum)
                    throw new ArgumentException("ToC/NIoPs array length is not suitable.");
                this.chunks = Enumerable.Range(0, chunkNum)
                    .Zip(tna, (_, t) => new { Index = _, ToPNIoPs = t })
                    .Select(_ => new PersistentChunk<TKey, TValue>(this,
                        GeneratePath(baseDirectoryPath, _.Index), _.ToPNIoPs.Item1, _.ToPNIoPs.Item2))
                    .ToArray();
            }
            else
            {
                this.chunks = Enumerable.Range(0, chunkNum)
                    .Select(_ => new PersistentChunk<TKey, TValue>(this, GeneratePath(baseDirectoryPath, _), comparer))
                    .ToArray();
            }
        }

        /// <summary>
        /// make sure path
        /// </summary>
        /// <param name="path"></param>
        private void EnsurePath(string path)
        {
            Directory.CreateDirectory(path);
        }

        private string GeneratePath(string basePath, int index)
        {
            return Path.Combine(basePath, index.ToString() + ".db");
        }

        /// <summary>
        /// Get amount of items.
        /// </summary>
        public override int Count
        {
            get
            {
                return chunks.Select(c => c.Count).Sum();
            }
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

        /// <summary>
        /// Get a item from key.
        /// </summary>
        /// <param name="key">find key</param>
        /// <returns>found item or empty.</returns>
        public override IObservable<TValue> Get(TKey key)
        {
            return GetChunk(key).Get(key);
        }

        /// <summary>
        /// Find items with a predicate.
        /// </summary>
        /// <param name="predicate">find predicate</param>
        /// <param name="range">finding id range</param>
        /// <param name="maxCountOfItems">max count of returning items</param>
        /// <returns>observable sequence</returns>
        public override IObservable<TValue> Find(Func<TValue, bool> predicate, FindRange<TKey> range = null, int? returnLowerBound = null)
        {
            return chunks.ToObservable()
                .SelectMany(c => c.Find(predicate, range, returnLowerBound));
        }

        /// <summary>
        /// Remove item from data store.
        /// </summary>
        /// <param name="key">removing item's key.</param>
        public override void Remove(TKey key)
        {
            GetChunk(key).Remove(key);
        }

        /// <summary>
        /// Get Table of Contents/Next Index of Packets enumerable.
        /// </summary>
        /// <returns>ToC/NIoPs</returns>
        public IEnumerable<Tuple<IDictionary<TKey, int>, IEnumerable<int>>> GetToCNIoPs()
        {
            return chunks.Select(c => Tuple.Create(c.GetTableOfContents(), c.GetNextIndexOfPacketsArray()));
        }

        private PersistentChunk<TKey, TValue> GetChunk(TKey key)
        {
            return chunks[Math.Abs(key.GetHashCode()) % chunkNum];
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            var tcol = chunks.Select(c => Task.Factory.StartNew(() => c.Dispose()));
            Task.WaitAll(tcol.ToArray());
        }
    }
}
