using System.Collections.Generic;

namespace StarryEyes.Mystique.Filters.Expressions.Values.BuiltIns
{
    public abstract class UserRepresentationBase
    {
        public abstract ICollection<long> User { get; }

        public abstract ICollection<long> Followings { get; }

        public abstract ICollection<long> Followers { get; }

        public abstract string ToQuery();
    }
}
