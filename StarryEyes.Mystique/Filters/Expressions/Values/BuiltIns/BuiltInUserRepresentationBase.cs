using System.Collections.Generic;

namespace StarryEyes.Mystique.Filters.Expressions.Values.BuiltIns
{
    public abstract class UserRepresentationBase
    {
        public abstract long UserId { get; }

        public abstract ICollection<long> Users { get; }

        public abstract ICollection<long> Followings { get; }

        public abstract ICollection<long> Followers { get; }

        public abstract string ToQuery();
    }
}
