using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace StarryEyes.Albireo.Collections
{
    public sealed class NotifyCollection<T> : ICollection<T>
    {
        private readonly ICollection<T> _source;
        private readonly Action _handler;

        public NotifyCollection(ICollection<T> source, Action handler)
        {
            Debug.Assert(source != null, "source != null");
            Debug.Assert(handler != null, "handler != null");
            _source = source;
            _handler = handler;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _source.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(T item)
        {
            _source.Add(item);
            _handler();
        }

        public void Clear()
        {
            _source.Clear();
            _handler();
        }

        public bool Contains(T item)
        {
            return _source.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _source.CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            return _source.Remove(item);
        }

        public int Count
        {
            get { return _source.Count; }
        }

        public bool IsReadOnly
        {
            get { return _source.IsReadOnly; }
        }
    }
}
