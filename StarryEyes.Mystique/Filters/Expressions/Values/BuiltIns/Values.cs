using System;
using System.Collections.Generic;
using StarryEyes.SweetLady.DataModel;

namespace StarryEyes.Mystique.Filters.Expressions.Values.BuiltIns
{
    public sealed class User : ValueBase
    {
        private UserRepresentationBase _representation;
        public User(UserRepresentationBase representation)
        {
            this._representation = representation;
        }

        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get
            {
                if (_representation.UserId != 0)
                    yield return FilterExpressionType.Numeric;
                yield return FilterExpressionType.Set;
            }
        }

        public override Func<TwitterStatus, long> GetNumericValueProvider()
        {
            if (_representation.UserId != 0)
                return _ => _representation.UserId;
            else
                return base.GetNumericValueProvider();
        }

        public override Func<TwitterStatus, ICollection<long>> GetSetValueProvider()
        {
            return _ => _representation.Users;
        }

        public override string ToQuery()
        {
            return _representation.ToQuery();
        }
    }

    public sealed class UserFollowings : ValueBase
    {
        private UserRepresentationBase _representation;
        public UserFollowings(UserRepresentationBase representation)
        {
            this._representation = representation;
        }

        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get { yield return FilterExpressionType.Set; }
        }

        public override Func<TwitterStatus, ICollection<long>> GetSetValueProvider()
        {
            return _ => _representation.Followings;
        }

        public override string ToQuery()
        {
            return _representation.ToQuery() + ".followings"; // "friends" also ok
        }
    }

    public sealed class UserFollowers : ValueBase
    {
        private UserRepresentationBase _representation;
        public UserFollowers(UserRepresentationBase representation)
        {
            this._representation = representation;
        }

        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get { yield return FilterExpressionType.Set; }
        }

        public override Func<TwitterStatus, ICollection<long>> GetSetValueProvider()
        {
            return _ => _representation.Followers;
        }

        public override string ToQuery()
        {
            return _representation.ToQuery() + ".followers";
        }
    }
}
