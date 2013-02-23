using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using StarryEyes.Vanille.Serialization;

namespace StarryEyes.Vanille.DataStore.Persistent
{
    /// <summary>
    ///     provide automated serialization service.
    /// </summary>
    /// <typeparam name="TKey">dictionary key</typeparam>
    /// <typeparam name="TValue">serialization content type</typeparam>
    public sealed class PersistentDataStore<TKey, TValue> : DataStoreBase<TKey, TValue>
        where TKey : IComparable<TKey>
        where TValue : IBinarySerializable, new()
    {
        private readonly int _chunkNum;
        private readonly PersistentChunk<TKey, TValue>[] _chunks;

        /// <summary>
        ///     Initialize Persistent Data Store
        /// </summary>
        /// <param name="keyProvider">key provider for the value</param>
        /// <param name="baseDirectoryPath">path for serialize objects</param>
        /// <param name="comparer">comparer for ordering key</param>
        /// <param name="chunkNum">cache separate count</param>
        /// <param name="manageData">ToC/NIoPs</param>
        public PersistentDataStore(Func<TValue, TKey> keyProvider, string baseDirectoryPath,
                                   IComparer<TKey> comparer = null,
                                   int chunkNum = 32,
                                   IEnumerable<Tuple<IDictionary<TKey, int>, IEnumerable<int>>> manageData = null)
            : base(keyProvider)
        {
            _chunkNum = chunkNum;
            EnsurePath(baseDirectoryPath);
            if (manageData != null)
            {
                var tna = manageData.ToArray();
                if (tna.Length != chunkNum)
                    throw new ArgumentException("ToC/NIoPs array length is not suitable.");
                _chunks = Enumerable.Range(0, chunkNum)
                                    .Zip(tna, (_, t) => new { Index = _, ToPNIoPs = t })
                                    .Select(_ => new PersistentChunk<TKey, TValue>(this,
                                                                                   GeneratePath(baseDirectoryPath,
                                                                                                _.Index), comparer,
                                                                                   _.ToPNIoPs.Item1, _.ToPNIoPs.Item2))
                                    .ToArray();
            }
            else
            {
                _chunks = Enumerable.Range(0, chunkNum)
                                    .Select(
                                        _ =>
                                        new PersistentChunk<TKey, TValue>(this, GeneratePath(baseDirectoryPath, _),
                                                                          comparer))
                                    .ToArray();
            }
        }


        /// <summary>
        ///     Number of chunks
        /// </summary>
        public int ChunkNum
        {
            get { return _chunkNum; }
        }

        /// <summary>
        ///     Get amount of items.
        /// </summary>
        public override int Count
        {
            get { return _chunks.Select(c => c.Count).Sum(); }
        }

        /// <summary>
        ///     make sure path
        /// </summary>
        /// <param name="path"></param>
        private void EnsurePath(string path)
        {
            Directory.CreateDirectory(path);
        }

        private string GeneratePath(string basePath, int index)
        {
            return Path.Combine(basePath, index.ToString(CultureInfo.InvariantCulture) + ".db");
        }

        /// <summary>
        ///     Add item or update item
        /// </summary>
        /// <param name="value">store item</param>
        public override void Store(TValue value)
        {
            TKey key = GetKey(value);
            GetChunk(key).AddOrUpdate(key, value);
        }

        /// <summary>
        ///     Get a item from key.
        /// </summary>
        /// <param name="key">find key</param>
        /// <returns>found item or empty.</returns>
        public override IObservable<TValue> Get(TKey key)
        {
            return GetChunk(key).Get(key);
        }

        /// <summary>
        ///     Find items with a predicate.
        /// </summary>
        /// <param name="predicate">find predicate</param>
        /// <param name="range">finding id range</param>
        /// <param name="itemCount">count of returning items</param>
        /// <returns>observable sequence</returns>
        public override IObservable<TValue> Find(Func<TValue, bool> predicate,
                                                 FindRange<TKey> range = null, int? itemCount = null)
        {
            return _chunks
                .ToObservable()
                .SelectMany(c => c.Find(predicate, range, itemCount));
        }

        /// <summary>
        ///     Remove item from data store.
        /// </summary>
        /// <param name="key">removing item's key.</param>
        public override void Remove(TKey key)
        {
            GetChunk(key).Remove(key);
        }

        /// <summary>
        ///     Get Table of Contents/Next Index of Packets enumerable.
        /// </summary>
        /// <returns>ToC/NIoPs</returns>
        public IEnumerable<Tuple<IDictionary<TKey, int>, IEnumerable<int>>> GetManageDatas()
        {
            return _chunks.Select(c => Tuple.Create(c.GetTableOfContents(), c.GetNextIndexOfPacketsArray()));
        }

        private PersistentChunk<TKey, TValue> GetChunk(TKey key)
        {
            return _chunks[Math.Abs(key.GetHashCode()) % _chunkNum];
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            var tcol = _chunks.Guard().Select(c => Task.Run((Action)c.Dispose));
            Task.WaitAll(tcol.ToArray());
        }
    }
}