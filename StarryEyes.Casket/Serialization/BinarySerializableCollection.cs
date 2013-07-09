using System.Collections.Generic;
using System.IO;

namespace StarryEyes.Casket.Serialization
{
    public sealed class BinarySerializableCollection<T> : ICollection<T>, IBinarySerializable where T : IBinarySerializable, new()
    {
        private List<T> _internalCollection = new List<T>();

        public void Add(T item)
        {
            this._internalCollection.Add(item);
        }

        public void Clear()
        {
            this._internalCollection.Clear();
        }

        public bool Contains(T item)
        {
            return this._internalCollection.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            this._internalCollection.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return this._internalCollection.Count; }
        }

        public bool IsReadOnly
        {
            get { return ((ICollection<T>)this._internalCollection).IsReadOnly; }
        }

        public bool Remove(T item)
        {
            return this._internalCollection.Remove(item);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return this._internalCollection.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(this._internalCollection);
        }

        public void Deserialize(BinaryReader reader)
        {
            this._internalCollection = new List<T>(reader.ReadCollection<T>());
        }
    }
}
