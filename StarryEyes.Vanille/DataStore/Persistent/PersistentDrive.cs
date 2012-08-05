using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using StarryEyes.Vanille.Serialization;

namespace StarryEyes.Vanille.DataStore.Persistent
{
    internal class PersistentDrive<TKey, TValue> : IDisposable where TValue : IBinarySerializable, new()
    {
        const int PacketSize = 256;
        const int PayloadSize = PacketSize - 33; // 33 bytes is a length of a header

        private string path;

        private Dictionary<TKey, int> tableOfContents = new Dictionary<TKey, int>();

        public PersistentDrive(string path, Dictionary<TKey, int> tableOfContents = null)
        {
            this.tableOfContents = tableOfContents ?? new Dictionary<TKey, int>();
            this.path = path;
        }

        public IDictionary<TKey, int> GetTableOfContents()
        {
            return tableOfContents;
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
