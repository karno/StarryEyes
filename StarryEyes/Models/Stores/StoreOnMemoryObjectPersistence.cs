using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using StarryEyes.Vanille.Serialization;

namespace StarryEyes.Models.Stores
{
    public static class StoreOnMemoryObjectPersistence
    {
        private static string GetDataStoreFileName(string key)
        {
            return Path.Combine(App.DataStorePath, key + ".dbm"); // database master
        }

        /// <summary>
        /// Persistent Table of Contents/Next Index of Packets enumerable.
        /// </summary>
        /// <param name="key">persistence key</param>
        /// <param name="tocniops">ToC/NIoP enumerable</param>
        public static void MakePersistent(string key, IEnumerable<Tuple<IDictionary<long, int>, IEnumerable<int>>> tocniops)
        {
            var sd = tocniops.Select(s => new ToCAndNIoP(s)).ToList();
            using (var fs = new FileStream(GetDataStoreFileName(key), FileMode.Create, FileAccess.ReadWrite))
            {
                BinarySerialization.SerializeCollection(fs, sd);
            }
        }

        public static bool IsPersistentDataExisted(string key)
        {
            return File.Exists(GetDataStoreFileName(key));
        }

        public static IEnumerable<Tuple<IDictionary<long, int>, IEnumerable<int>>> GetPersistentData(string key)
        {
            using (var fs = new FileStream(GetDataStoreFileName(key), FileMode.Open, FileAccess.ReadWrite))
            {
                return BinarySerialization.DeserializeCollection<ToCAndNIoP>(fs)
                    .Select(s => s.GetToCNIoP());
            }
        }
    }

    public class ToCAndNIoP : IBinarySerializable
    {
        public ToCAndNIoP()
        {
            TableOfContents = new Dictionary<long, int>();
            NextIndexOfPackets = new List<int>();
        }

        public ToCAndNIoP(Tuple<IDictionary<long, int>, IEnumerable<int>> tocniop)
        {
            TableOfContents = new Dictionary<long, int>(tocniop.Item1);
            NextIndexOfPackets = new List<int>(tocniop.Item2);
            OptimizeNextIndexOfPackets();
        }

        private void OptimizeNextIndexOfPackets()
        {
            // -1 is empty record.

            int idx = NextIndexOfPackets.Count - 1;
            // determine first not -1 index from last
            while (idx >= 0 && NextIndexOfPackets[idx] == -1)
                idx--;
            if (idx == -1)
            {
                // not found
                NextIndexOfPackets.Clear();
                return;
            }
            else
            {
                idx++;
                // trim tail '-1's
                if (idx > 0 && idx < NextIndexOfPackets.Count)
                    NextIndexOfPackets.RemoveRange(idx, NextIndexOfPackets.Count - idx);
            }
        }

        public Dictionary<long, int> TableOfContents { get; set; }

        public List<int> NextIndexOfPackets { get; set; }

        public Tuple<IDictionary<long, int>, IEnumerable<int>> GetToCNIoP()
        {
            return new Tuple<IDictionary<long, int>, IEnumerable<int>>(TableOfContents, NextIndexOfPackets);
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(TableOfContents.Count);
            TableOfContents.ForEach(kvp =>
            {
                writer.Write(kvp.Key);
                writer.Write(kvp.Value);
            });
            writer.Write(NextIndexOfPackets.Count);
            NextIndexOfPackets.ForEach(i => writer.Write(i));
        }

        public void Deserialize(BinaryReader reader)
        {
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                var key = reader.ReadInt64();
                var value = reader.ReadInt32();
                TableOfContents.Add(key, value);
            }
            count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                NextIndexOfPackets.Add(reader.ReadInt32());
            }
        }
    }
}
