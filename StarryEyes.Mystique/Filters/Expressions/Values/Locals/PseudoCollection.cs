using System;
using System.Collections.Generic;

namespace StarryEyes.Mystique.Filters.Expressions.Values.Locals
{
    class PseudoCollection<T> : ICollection<T>
    {
        Func<T, bool> containsFunc;
        public PseudoCollection(Func<T, bool> containsFunc)
        {
            this.containsFunc = containsFunc;
        }

        public void Add(T item)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(T item)
        {
            return containsFunc(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public int Count
        {
            get { throw new NotImplementedException(); }
        }

        public bool IsReadOnly
        {
            get { return true; }
        }

        public bool Remove(T item)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<T> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}
