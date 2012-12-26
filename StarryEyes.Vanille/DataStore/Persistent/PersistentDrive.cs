using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using StarryEyes.Vanille.Serialization;

namespace StarryEyes.Vanille.DataStore.Persistent
{
    internal class PersistentDrive<TKey, TValue> : IDisposable
        where TKey : IComparable<TKey>
        where TValue : IBinarySerializable, new()
    {
        const int PacketSize = 256;
        const int ChunkSize = 1024;

        const int EndOfPackets = 0;
        const int Empty = -1;

        private readonly string _path;
        public string Path
        {
            get { return _path; }
        }

        private readonly object _fslock = new object();
        private FileStream _fstream;

        // niop is delimited per 1024 packets(= 1 chunks).
        private readonly SortedDictionary<int, List<int>> _nextIndexOfPackets;

        // start index resolver
        private readonly SortedDictionary<TKey, int> _tableOfContents;

        /// <summary>
        /// Initialize persistent drive with create-new mode.
        /// </summary>
        /// <param name="path">base file path</param>
        /// <param name="comparer">comparer for key ordering</param>
        public PersistentDrive(string path, IComparer<TKey> comparer)
        {
            this._tableOfContents = new SortedDictionary<TKey, int>(comparer);
            this._nextIndexOfPackets = new SortedDictionary<int, List<int>>();
            SetNextIndexOfPackets(0, EndOfPackets); // index 0 is reserved for the parity.
            this._path = path;
            PrepareFile(false);
        }

        /// <summary>
        /// Initialize persistent drive with load previous data mode.
        /// </summary>
        /// <param name="path">base file path</param>
        /// <param name="tableOfContents">previous table of contents</param>
        /// <param name="nextIndexOfPackets">previous next index of packets table</param>
        public PersistentDrive(string path, IDictionary<TKey, int> tableOfContents, IEnumerable<int> nextIndexOfPackets)
        {
            this._tableOfContents = new SortedDictionary<TKey, int>(tableOfContents);
            this._nextIndexOfPackets = new SortedDictionary<int, List<int>>();
            nextIndexOfPackets
                .Zip(Enumerable.Range(0, Int32.MaxValue), (v, k) => new { Key = k, Value = v })
                .ForEach(i => SetNextIndexOfPackets(i.Key, i.Value));
            this._path = path;
            PrepareFile(true);
        }

        /// <summary>
        /// Get amount of stored items.
        /// </summary>
        public int Count
        {
            get { return _tableOfContents.Count; }
        }

        /// <summary>
        /// Prepare file for storage
        /// </summary>
        /// <param name="isInitializedWithToc">load-mode flag</param>
        private void PrepareFile(bool isInitializedWithToc)
        {
            if (isInitializedWithToc)
            {
                _fstream = File.Open(_path, FileMode.Open, FileAccess.ReadWrite);
                // verify toc and niop-table

                if (!VerifyToCNIoPParity(LoadInternal(0)))
                {
                    // if invalid, throw exception
                    _fstream.Close();
                    throw new DataPersistenceException("Index table verification failed.");
                }
            }
            else
            {
                // initialize file by empty data.
                _fstream = File.Open(_path, FileMode.Create, FileAccess.ReadWrite);
            }
        }

        /// <summary>
        /// Get ToC/NIoP Parity bytes.
        /// </summary>
        /// <returns>parity</returns>
        private byte[] GetToCNIoPParity()
        {
            // 64bit
            long tocParity = 0;
            _tableOfContents
                .Select(kvp => (long)kvp.Value)
                .ForEach(i => tocParity ^= i << (int)(i % 5));
            // 64bit
            long niopParity = 0;
            _nextIndexOfPackets
                .SelectMany(kvp => kvp.Value.Select(v => (long)(v + kvp.Key * ChunkSize)))
                .ForEach(i => niopParity ^= i << (int)(i % 5));
            return BitConverter.GetBytes(tocParity).Concat(BitConverter.GetBytes(niopParity)).ToArray();
        }

        /// <summary>
        /// Verify ToC/NIoP.
        /// </summary>
        /// <param name="parity">parity</param>
        /// <returns>if verified, return true</returns>
        private bool VerifyToCNIoPParity(byte[] parity)
        {
            // 128bit parity
            var comparate = GetToCNIoPParity();
            return comparate.SequenceEqual(parity.Take(16));
        }

        /// <summary>
        /// Set next index for specified index
        /// </summary>
        /// <param name="index">set index</param>
        /// <param name="next">next index</param>
        private void SetNextIndexOfPackets(int index, int next)
        {
            // next index of chunks table is delimited by ChunkSize.
            // every chunks is under controlled by mother tree(nextIndexOfPackets).
            int chunk = index / ChunkSize;
            int key = index % ChunkSize;
            List<int> curChunk;

            // get chunk
            if (this._nextIndexOfPackets.TryGetValue(chunk, out curChunk))
            {
                // chunk found
                curChunk[key] = next;
            }
            else if (next != Empty) // if a chunk is not exited, niops in a chunk are treated as empty.
            {
                // chunk not found, create new
                var list = new List<int>(Enumerable.Repeat(Empty, ChunkSize)); // init chunk with Empty recode
                list[key] = next;
                // add chunk to niop-tree
                this._nextIndexOfPackets[chunk] = list;
            }
        }

        /// <summary>
        /// Get next index for specified index
        /// </summary>
        /// <param name="index">source index</param>
        /// <returns>next index</returns>
        private int GetNextIndexOfPackets(int index)
        {
            int chunk = index / ChunkSize;
            int key = index % ChunkSize;
            List<int> curChunk;
            if (this._nextIndexOfPackets.TryGetValue(chunk, out curChunk))
            {
                return curChunk[key];
            }
            // chunk is not existed yet.
            // -> treat as empty.
            return Empty;
        }

        /// <summary>
        /// Get Next Index of Packets.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<int> GetNextIndexOfPackets()
        {
            return Enumerable.Range(0, _nextIndexOfPackets.Keys.Max() + 1)
                .Select(i => _nextIndexOfPackets.ContainsKey(i) ? _nextIndexOfPackets[i] : Enumerable.Repeat(Empty, ChunkSize))
                .SelectMany(_ => _);
        }

        /// <summary>
        /// Get Table of Contents.
        /// </summary>
        public IDictionary<TKey, int> GetTableOfContents()
        {
            return _tableOfContents;
        }

        /// <summary>
        /// Store a value.
        /// </summary>
        /// <param name="key">storing value's key</param>
        /// <param name="value">store value</param>
        public void Store(TKey key, TValue value)
        {
            // remove old data, if existed.
            Remove(key);

            byte[] bytes;
            // serialize data
            using (var ms = new MemoryStream())
            using (var cs = new MemoryStream())
            using (var bw = new BinaryWriter(ms))
            {
                // serialize
                value.Serialize(bw);
                bw.Flush();

                ms.Seek(0, SeekOrigin.Begin); // initialize seek point
                cs.Seek(4, SeekOrigin.Begin); // 4 bytes for length placeholder.
                //compress
                using (var ds = new DeflateStream(cs, CompressionLevel.Fastest))
                {
                    ms.CopyTo(ds);
                    ms.Flush();
                }
                bytes = cs.ToArray();
            }

            // get length-bit into the header
            var lengthBytes = BitConverter.GetBytes(bytes.Length - 4); // byte[4]

            // paste length bytes into serialize buffer
            Array.Copy(lengthBytes, bytes, 4);

            // get index for storing data
            int writeTo = GetNextEmptyIndex(0);
            _tableOfContents[key] = writeTo;

            // store
            StoreInternal(bytes, writeTo);
        }

        /// <summary>
        /// Load a value from key.<para />
        /// If key is not found, throws KeyNotFoundException.
        /// </summary>
        /// <param name="key">finding key</param>
        /// <returns>value</returns>
        public TValue Load(TKey key)
        {
            int readFrom;
            if (!_tableOfContents.TryGetValue(key, out readFrom) || readFrom < 0)
                throw new KeyNotFoundException("Not found key in this persistent drive.");
            return Load(readFrom);
        }

        /// <summary>
        /// Find values with predicate.
        /// </summary>
        /// <param name="predicate">find predicate</param>
        /// <returns>value sequence</returns>
        public IEnumerable<TValue> Find(Func<TValue, bool> predicate, FindRange<TKey> range, int? returnLowerBound)
        {
            IEnumerable<int> indexes;
            if (range != null)
            {
                indexes = _tableOfContents
                    .CheckRange(range, _ => _.Key)
                    .Select(_ => _.Value);
            }
            else
            {
                indexes = _tableOfContents.Values;
            }
            return indexes.Select(Load)
                .Where(predicate)
                .TakeIfNotNull(returnLowerBound);
        }

        /// <summary>
        /// Load from index.
        /// </summary>
        /// <param name="index">offset index</param>
        /// <returns>deserialized value</returns>
        private TValue Load(int index)
        {
            /*
            try
            {
            */
            var bytes = LoadInternal(index);
            var length = BitConverter.ToInt32(bytes, 0);
            var ret = new TValue();
            // decompress
            using (var cs = new MemoryStream(bytes, 4, length, false))
            using (var ds = new DeflateStream(cs, CompressionMode.Decompress))
            using (var ms = new MemoryStream())
            {
                ds.CopyTo(ms);
                ds.Flush();

                ms.Seek(0, SeekOrigin.Begin);
                using (var br = new BinaryReader(ms))
                {
                    ret.Deserialize(br);
                }
            }
            return ret;
            /*
            }
            catch (Exception ex)
            {
                throw new DataPersistenceException("data load error.", ex);
            }
            */
        }

        /// <summary>
        /// Remove from store.
        /// </summary>
        /// <param name="key">removing key</param>
        /// <returns>succeeded or not</returns>
        public bool Remove(TKey key)
        {
            int idx;
            if (!_tableOfContents.TryGetValue(key, out idx))
                return false; // not found
            do
            {
                // clear all packets table.
                var newidx = GetNextIndexOfPackets(idx);
                if (newidx == Empty) return false; // already removed
                SetNextIndexOfPackets(idx, Empty);
                idx = newidx;
            } while (idx > 0);
            // OK
            return true;
        }

        /// <summary>
        /// Store value.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        private void StoreInternal(byte[] data, int offset)
        {
            // current writing data starting offset
            int cursor = 0;

            // write packets
            while (true)
            {
                lock (_fslock)
                {
                    // seek to write destination, write packet
                    _fstream.Seek(offset * PacketSize, SeekOrigin.Begin);
                    if (data.Length - cursor < PacketSize)
                    {
                        _fstream.Write(data, cursor, data.Length - cursor);
                    }
                    else
                    {
                        _fstream.Write(data, cursor, PacketSize);
                    }
                }

                // move cursor
                cursor += PacketSize;

                if (cursor > data.Length)
                {
                    lock (_fslock)
                    {
                        _fstream.Flush();
                    }
                    SetNextIndexOfPackets(offset, EndOfPackets); // finalize (mark as EOP)
                    return; // write completed
                }

                // get next writable offset
                var newOffset = GetNextEmptyIndex(offset);
                SetNextIndexOfPackets(offset, newOffset);
                offset = newOffset; // move to next
            }
        }

        /// <summary>
        /// Get next empty index of niop.
        /// </summary>
        /// <param name="start">find start packet</param>
        /// <returns>larger than find start packet.</returns>
        private int GetNextEmptyIndex(int start)
        {
            int current = start + 1;
            while (true)
            {
                if (GetNextIndexOfPackets(current) == Empty)
                    break;
                current++;
            }
            return current;
        }

        /// <summary>
        /// Load bytes array.
        /// </summary>
        /// <param name="offset">load start offset</param>
        /// <returns>byte array</returns>
        private byte[] LoadInternal(int offset)
        {
            // data reading buffer
            var buffer = new byte[PacketSize];
            // return data buffer
            using (var ms = new MemoryStream())
            {
                while (true)
                {
                    int read;
                    lock (_fslock)
                    {
                        // seek to source offset, read packet
                        _fstream.Seek(offset * PacketSize, SeekOrigin.Begin);
                        read = _fstream.Read(buffer, 0, PacketSize);
                    }

                    // write to return stream
                    ms.Write(buffer, 0, read);

                    // determine next packet index
                    offset = GetNextIndexOfPackets(offset);
                    if (offset == 0)
                        return ms.ToArray(); // return 'return stream' as array
                }
            }
        }

        /// <summary>
        /// Optimize the data store.
        /// </summary>
        public void Optimize()
        {
            // TODO: Impl
            throw new NotImplementedException();
        }

        /// <summary>
        /// Store the parity bits.
        /// </summary>
        public void StoreParity()
        {
            StoreInternal(GetToCNIoPParity(), 0);
        }

        public void Dispose()
        {
            StoreParity();
            lock (_fslock)
            {
                _fstream.Flush();
                _fstream.Close();
                _fstream.Dispose();
                _fstream = null;
            }
        }
    }
}
