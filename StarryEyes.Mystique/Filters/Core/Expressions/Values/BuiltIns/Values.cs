using System;
using System.Collections.Generic;
using StarryEyes.SweetLady.DataModel;

namespace StarryEyes.Mystique.Filters.Core.Expressions.Values.BuiltIns
{
    public sealed class User : ValueBase
    {
        public override KQExpressionType[] TransformableTypes
        {
            get { throw new NotImplementedException(); }
        }

        public override string ToQuery()
        {
            throw new NotImplementedException();
        }
    }


    public sealed class UserFollowings : ValueBase
    {
        private BuiltInUserRepresentationBase _representation;
        public UserFollowings(BuiltInUserRepresentationBase representation)
        {
            this._representation = representation;
        }

        public override KQExpressionType[] TransformableTypes
        {
            get { return new[] { KQExpressionType.Set }; }
        }

        public override ICollection<long> GetSetValue(TwitterStatus @unused)
        {
            return _representation.Followings;
        }

        public override string ToQuery()
        {
            return _representation.ToQuery() + ".followings"; // "friends" also ok
        }
    }

    public sealed class UserFollowers : ValueBase
    {
        private BuiltInUserRepresentationBase _representation;
        public UserFollowers(BuiltInUserRepresentationBase representation)
        {
            this._representation = representation;
        }

        public override KQExpressionType[] TransformableTypes
        {
            get { return new[] { KQExpressionType.Set }; }
        }

        public override ICollection<long> GetSetValue(TwitterStatus @unused)
        {
            return _representation.Followers;
        }

        public override string ToQuery()
        {
            return _representation.ToQuery() + ".followers";
        }
    }
}
