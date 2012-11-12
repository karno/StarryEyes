using System;
using System.Collections.Generic;
using StarryEyes.Breezy.DataModel;

namespace StarryEyes.Filters.Expressions.Values.Locals
{
    public sealed class LocalUser : ValueBase
    {
        private UserRepresentationBase _representation;
        public LocalUser(UserRepresentationBase representation)
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

        public override Func<TwitterStatus, IReadOnlyCollection<long>> GetSetValueProvider()
        {
            return _ => _representation.Users;
        }

        public override string ToQuery()
        {
            return _representation.ToQuery();
        }
    }

    public sealed class LocalUserFollowings : ValueBase
    {
        private UserRepresentationBase _representation;
        public LocalUserFollowings(UserRepresentationBase representation)
        {
            this._representation = representation;
        }

        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get { yield return FilterExpressionType.Set; }
        }

        public override Func<TwitterStatus, IReadOnlyCollection<long>> GetSetValueProvider()
        {
            return _ => _representation.Followings;
        }

        public override string ToQuery()
        {
            return _representation.ToQuery() + ".followings"; // "friends" also ok
        }
    }

    public sealed class LocalUserFollowers : ValueBase
    {
        private UserRepresentationBase _representation;
        public LocalUserFollowers(UserRepresentationBase representation)
        {
            this._representation = representation;
        }

        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get { yield return FilterExpressionType.Set; }
        }

        public override Func<TwitterStatus, IReadOnlyCollection<long>> GetSetValueProvider()
        {
            return _ => _representation.Followers;
        }

        public override string ToQuery()
        {
            return _representation.ToQuery() + ".followers";
        }
    }
}
