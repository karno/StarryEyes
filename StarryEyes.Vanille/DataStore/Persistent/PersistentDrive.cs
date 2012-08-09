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

        public PersistentDrive(string path)
        {
            this.tableOfContents = new SortedDictionary<TKey, int>();
            this.nextIndexOfPackets = new SortedDictionary<int, List<int>>();
            this.path = path;
            PrepareFile(false);
        }

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

        private void SetNextIndexOfPackets(int index, int next)
        {
            int chunk = index / ChunkSize;
            int key = index % ChunkSize;
            List<int> curChunk;
            if (this.nextIndexOfPackets.TryGetValue(chunk, out curChunk))
            {
                curChunk[key] = next;
            }
            else
            {
                var list = new List<int>(Enumerable.Range(Empty, ChunkSize));
                list[key] = next;
                this.nextIndexOfPackets.Add(chunk, list);
            }
        }

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
                return EndOfIndexOfPackets;
            }
        }

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
                // initialize file
                fs = File.Open(path, FileMode.Create, FileAccess.ReadWrite);
            }
        }

        public IDictionary<TKey, int> GetTableOfContents()
        {
            return tableOfContents;
        }

        public void Store(TKey key, TValue value)
        {
            var ms = new MemoryStream();
            using (var bw = new BinaryWriter(ms))
                value.Serialize(bw);
            var length = (int)ms.Length;
            var lengthBytes = BitConverter.GetBytes(length); // byte[4]
            byte[] writeBuf = new byte[length + 4];
            Array.Copy(lengthBytes, writeBuf, 4);
            Array.Copy(ms.ToArray(), 0, writeBuf, 4, ms.Length);
            int writeTo = GetNextEmptyIndex(0);
            tableOfContents.Add(key, writeTo);
            StoreInternal(writeBuf, writeTo);
        }

        public TValue Load(TKey key)
        {
            int readFrom;
            if (!tableOfContents.TryGetValue(key, out readFrom))
                throw new KeyNotFoundException("Not found key in this persistent drive.");
            return Load(readFrom);
        }

        public IEnumerable<TValue> Find(Func<TValue, bool> predicate)
        {
            return tableOfContents.Values
                .Select(Load)
                .Where(predicate);
        }

        private TValue Load(int index)
        {
            var bytes = LoadInternal(index);
            int length = BitConverter.ToInt32(bytes, 0);
            var ret = new TValue();
            using (var br = new BinaryReader(new MemoryStream(bytes, 4, length, false)))
                ret.Deserialize(br);
            return ret;
        }

        public bool Remove(TKey key)
        {
            int idx;
            if(!tableOfContents.TryGetValue(key, out idx))
                return false;
            do
            {
                var newidx = GetNextIndexOfPackets(idx);
                SetNextIndexOfPackets(idx, Empty);
            } while (idx != 0);
            return true;
        }

        private void StoreInternal(byte[] data, int startOffset)
        {
            // current writing data starting offset
            int cursor = 0;

            // set offset
            int offset = startOffset;

            while (true)
            {
                fs.Seek(offset * PacketSize, SeekOrigin.Begin);
                fs.Write(data, cursor, Math.Min(PacketSize, data.Length - cursor));

                cursor += PacketSize;

                if (cursor < data.Length)
                {
                    SetNextIndexOfPackets(offset, 0);
                    break; // write completed
                }

                // get next writable offset
                var newOffset = GetNextEmptyIndex(offset);
                SetNextIndexOfPackets(offset, newOffset);
            }
        }

        private int GetNextEmptyIndex(int start)
        {
            int current = start + 1;
            while (true)
            {
                switch (GetNextIndexOfPackets(current))
                {
                    case Empty:
                        return current;
                    case EndOfIndexOfPackets:
                        return nextIndexOfPackets.Count;
                    default:
                        current++;
                        break;
                }
            }
        }

        private byte[] LoadInternal(int offset)
        {
            // current capacity packets of buffer
            int capacity = PacketSize;
            // return data buffer
            var buffer = new byte[PacketSize];
            // writing target
            int cursor = 0;

            while (true)
            {
                fs.Seek(offset * PacketSize, SeekOrigin.Begin);
                if (fs.Read(buffer, cursor, PacketSize) != PacketSize) //  corrupted
                    throw new IOException("Internal File System is corrupted!");
                cursor += PacketSize;

                // determine next packet index
                offset = GetNextIndexOfPackets(offset);
                if (offset == 0)
                    return buffer;

                // expand buffer
                if (cursor >= PacketSize)
                {
                    capacity *= 2;
                    var newBuffer = new byte[capacity];
                    buffer.CopyTo(newBuffer, 0);
                    buffer = newBuffer;
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

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
