using System.Collections.Generic;
using System.IO;

namespace StarryEyes.Vanille.Serialization
{
    public sealed class BinarySerializableCollection<T> : ICollection<T>, IBinarySerializable where T: IBinarySerializable, new()
    {
        private List<T> internalCollection = new List<T>();

        public void Add(T item)
        {
            internalCollection.Add(item);
        }

        public void Clear()
        {
            internalCollection.Clear();
        }

        public bool Contains(T item)
        {
            return internalCollection.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            internalCollection.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return internalCollection.Count; }
        }

        public bool IsReadOnly
        {
            get { return ((ICollection<T>)internalCollection).IsReadOnly; }
        }

        public bool Remove(T item)
        {
            return internalCollection.Remove(item);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return internalCollection.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(internalCollection);
        }

        public void Deserialize(BinaryReader reader)
        {
            internalCollection = new List<T>(reader.ReadCollection<T>());
        }
    }
}
