using System.Collections.Generic;

namespace StarryEyes.Models.Stores.Internal
{
    public class IdReverseComparer : IComparer<long>
    {
        public int Compare(long x, long y)
        {
            return x == y ? 0 : (x > y ? -1 : 1);
        }
    }
}
