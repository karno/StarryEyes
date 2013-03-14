using System.Collections.Generic;

namespace StarryEyes.Filters.Expressions.Values.Locals
{
    public abstract class UserExpressionBase
    {
        public abstract long UserId { get; }

        public abstract IReadOnlyCollection<long> Users { get; }

        public abstract IReadOnlyCollection<long> Followings { get; }

        public abstract IReadOnlyCollection<long> Followers { get; }

        public abstract string ToQuery();
    }
}
