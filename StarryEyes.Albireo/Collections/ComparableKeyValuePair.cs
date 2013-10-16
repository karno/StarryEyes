using System;

namespace StarryEyes.Albireo.Collections
{
    public class ComparableKeyValuePair<TKey, TValue>
        : IComparable<TKey> where TKey : IComparable<TKey>
    {
        public TKey Key { get; set; }

        public TValue Value { get; set; }

        public int CompareTo(TKey other)
        {
            return this.Key.CompareTo(other);
        }
    }
}
