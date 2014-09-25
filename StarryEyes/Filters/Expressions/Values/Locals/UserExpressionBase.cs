using System;
using System.Collections.Generic;
using StarryEyes.Albireo.Helpers;
using StarryEyes.Models.Accounting;

namespace StarryEyes.Filters.Expressions.Values.Locals
{
    public abstract class UserExpressionBase
    {
        public abstract long UserId { get; }

        public abstract IReadOnlyCollection<long> Users { get; }

        public abstract IReadOnlyCollection<long> Followings { get; }

        public abstract IReadOnlyCollection<long> Followers { get; }

        public abstract IReadOnlyCollection<long> Blockings { get; }

        public abstract IReadOnlyCollection<long> Mutes { get; }

        public abstract string UserIdSql { get; }

        public abstract string UsersSql { get; }

        public abstract string FollowingsSql { get; }

        public abstract string FollowersSql { get; }

        public abstract string BlockingsSql { get; }

        public abstract string MutesSql { get; }

        public abstract string ToQuery();

        public virtual void BeginLifecycle() { }

        public virtual void EndLifecycle() { }

        protected void RaiseReapplyFilter(RelationDataChangedInfo relInfo)
        {
            this.ReapplyRequested.SafeInvoke(relInfo);
        }

        public event Action<RelationDataChangedInfo> ReapplyRequested;
    }
}
