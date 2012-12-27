using System.Collections.Generic;
using System.IO;

namespace StarryEyes.Vanille.Serialization
{
    public sealed class BinarySerializableCollection<T> : ICollection<T>, IBinarySerializable where T : IBinarySerializable, new()
    {
        private List<T> _internalCollection = new List<T>();

        public void Add(T item)
        {
            _internalCollection.Add(item);
        }

        public void Clear()
        {
            _internalCollection.Clear();
        }

        public bool Contains(T item)
        {
            return _internalCollection.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _internalCollection.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return _internalCollection.Count; }
        }

        public bool IsReadOnly
        {
            get { return ((ICollection<T>)_internalCollection).IsReadOnly; }
        }

        public bool Remove(T item)
        {
            return _internalCollection.Remove(item);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _internalCollection.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(_internalCollection);
        }

        public void Deserialize(BinaryReader reader)
        {
            _internalCollection = new List<T>(reader.ReadCollection<T>());
        }
    }
}
