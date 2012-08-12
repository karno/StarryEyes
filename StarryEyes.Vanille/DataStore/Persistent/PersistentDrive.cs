using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using StarryEyes.Vanille.Serialization;

namespace StarryEyes.Vanille.DataStore.Persistent
{
    internal class PersistentDrive<TKey, TValue> : IDisposable where TValue : IBinarySerializable, new()
    {
        const int PacketSize = 512;
        const int ChunkSize = 1024;

        const int Empty = -1;
        const int EndOfIndexOfPackets = -2;

        private string path;
        public string Path
        {
            get { return path; }
        }

        private FileStream fs;

        // niop is delimited per 1024 packets(= 1 chunks).
        private SortedDictionary<int, List<int>> nextIndexOfPackets;
        private SortedDictionary<TKey, int> tableOfContents;

        /// <summary>
        /// Initialize persistent drive with create-new mode.
        /// </summary>
        /// <param name="path">base file path</param>
        public PersistentDrive(string path)
        {
            this.tableOfContents = new SortedDictionary<TKey, int>();
            this.nextIndexOfPackets = new SortedDictionary<int, List<int>>();
            this.path = path;
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
            this.tableOfContents = new SortedDictionary<TKey, int>(tableOfContents);
            this.nextIndexOfPackets = new SortedDictionary<int, List<int>>();
            nextIndexOfPackets
                .Zip(Enumerable.Range(0, Int32.MaxValue), (v, k) => new { Key = k, Value = v })
                .ForEach(i => SetNextIndexOfPackets(i.Key, i.Value));
            this.path = path;
            PrepareFile(true);
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
            if (this.nextIndexOfPackets.TryGetValue(chunk, out curChunk))
            {
                // chunk found
                curChunk[key] = next;
            }
            else
            {
                // chunk not found, create new
                var list = new List<int>(Enumerable.Range(Empty, ChunkSize)); // init chunk with Empty recode
                list[key] = next;
                // add chunk to niop-tree
                this.nextIndexOfPackets.Add(chunk, list);
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
            if (this.nextIndexOfPackets.TryGetValue(chunk, out curChunk))
            {
                return curChunk[key];
            }
            else
            {
                // pseudo END (data corrupted?)
                return EndOfIndexOfPackets;
            }
        }

        /// <summary>
        /// Prepare file for storage
        /// </summary>
        /// <param name="isInitializedWithToc">load-mode flag</param>
        private void PrepareFile(bool isInitializedWithToc)
        {
            if (isInitializedWithToc)
            {
                fs = File.Open(path, FileMode.Open, FileAccess.ReadWrite);
                // verify toc and niop-table

                // TODO: use parity, it is existed at index #0.

                // if invalid, throw exception
                // fs.Close();
                // throw new IOException("Index table verification failed.");
            }
            else
            {
                // initialize file by empty data.
                fs = File.Open(path, FileMode.Create, FileAccess.ReadWrite);
            }
        }

        /// <summary>
        /// Get Next Index of Packets.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<int> GetNextIndexOfPackets()
        {
            return Enumerable.Range(0, nextIndexOfPackets.Keys.Max())
                .Select(i => nextIndexOfPackets.ContainsKey(i) ? nextIndexOfPackets[i] : Enumerable.Repeat(Empty, ChunkSize))
                .SelectMany(_ => _);
        }

        /// <summary>
        /// Get Table of Contents.
        /// </summary>
        public IDictionary<TKey, int> GetTableOfContents()
        {
            return tableOfContents;
        }

        /// <summary>
        /// Store a value.
        /// </summary>
        /// <param name="key">storing value's key</param>
        /// <param name="value">store value</param>
        public void Store(TKey key, TValue value)
        {
            // serialize data
            var ms = new MemoryStream();
            ms.Write(new byte[4], 0, 4); // add empty 4 bytes (placeholder)
            using (var bw = new BinaryWriter(ms))
                value.Serialize(bw);
            var bytes = ms.ToArray();

            // get length-bit into the header
            var lengthBytes = BitConverter.GetBytes(bytes.Length - 4); // byte[4]

            // paste length bytes into serialize buffer
            Array.Copy(lengthBytes, bytes, 4);

            // get index for storing data
            int writeTo = GetNextEmptyIndex(0);
            tableOfContents.Add(key, writeTo);

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
            if (!tableOfContents.TryGetValue(key, out readFrom))
                throw new KeyNotFoundException("Not found key in this persistent drive.");
            return Load(readFrom);
        }

        /// <summary>
        /// Find values with predicate.
        /// </summary>
        /// <param name="predicate">find predicate</param>
        /// <returns>value sequence</returns>
        public IEnumerable<TValue> Find(Func<TValue, bool> predicate)
        {
            return tableOfContents.Values
                .Select(Load)
                .Where(predicate);
        }

        /// <summary>
        /// Load from index.
        /// </summary>
        /// <param name="index">offset index</param>
        /// <returns>deserialized value</returns>
        private TValue Load(int index)
        {
            var bytes = LoadInternal(index);
            int length = BitConverter.ToInt32(bytes, 0);
            var ret = new TValue();
            using (var br = new BinaryReader(new MemoryStream(bytes, 4, length, false)))
                ret.Deserialize(br);
            return ret;
        }

        /// <summary>
        /// Remove from store.
        /// </summary>
        /// <param name="key">removing key</param>
        /// <returns>succeeded or not</returns>
        public bool Remove(TKey key)
        {
            int idx;
            if(!tableOfContents.TryGetValue(key, out idx))
                return false; // not found
            do
            {
                // clear all packets table.
                var newidx = GetNextIndexOfPackets(idx);
                SetNextIndexOfPackets(idx, Empty);
            } while (idx != 0);
            // OK
            return true;
        }

        /// <summary>
        /// Store value.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="startOffset"></param>
        private void StoreInternal(byte[] data, int startOffset)
        {
            // current writing data starting offset
            int cursor = 0;

            // set offset
            int offset = startOffset;

            // write packets
            while (true)
            {
                // seek to write destination, write packet
                fs.Seek(offset * PacketSize, SeekOrigin.Begin);
                fs.Write(data, cursor, Math.Min(PacketSize, data.Length - cursor));

                // move cursor
                cursor += PacketSize;

                if (cursor < data.Length)
                {
                    SetNextIndexOfPackets(offset, 0); // finalize (mark as EOP)
                    break; // write completed
                }

                // get next writable offset
                var newOffset = GetNextEmptyIndex(offset);
                SetNextIndexOfPackets(offset, newOffset);
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
                switch (GetNextIndexOfPackets(current))
                {
                    case Empty: // empty block
                        return current;
                    case EndOfIndexOfPackets: // EIOP
                        return nextIndexOfPackets.Count; // next block
                    default:
                        current++; // continue loop
                        break;
                }
            }
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
            var rstream = new MemoryStream(); 

            // read size
            int read = 0;

            while (true)
            {
                // seek to source offset, read packet
                fs.Seek(offset * PacketSize, SeekOrigin.Begin);
                read = fs.Read(buffer, 0, PacketSize);

                // write to return stream
                rstream.Write(buffer, 0, read);

                // determine next packet index
                offset = GetNextIndexOfPackets(offset);
                if (offset == 0)
                    return rstream.ToArray(); // return 'return stream' as array
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

        public void Dispose()
        {
            fs.Flush();
            fs.Close();
            fs = null;
        }
    }
}
