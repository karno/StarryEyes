using System;
using System.Collections.Generic;
using StarryEyes.Breezy.DataModel;

namespace StarryEyes.Filters.Expressions.Values.Locals
{
    public sealed class LocalUser : ValueBase
    {
        private UserExpressionBase _expression;
        public LocalUser(UserExpressionBase expression)
        {
            this._expression = expression;
        }

        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get
            {
                if (_expression.UserId != 0)
                    yield return FilterExpressionType.Numeric;
                yield return FilterExpressionType.Set;
            }
        }

        public override Func<TwitterStatus, long> GetNumericValueProvider()
        {
            if (_expression.UserId != 0)
                return _ => _expression.UserId;
            else
                return base.GetNumericValueProvider();
        }

        public override Func<TwitterStatus, IReadOnlyCollection<long>> GetSetValueProvider()
        {
            return _ => _expression.Users;
        }

        public override string ToQuery()
        {
            return _expression.ToQuery();
        }
    }

    public sealed class LocalUserFollowings : ValueBase
    {
        private UserExpressionBase _expression;
        public LocalUserFollowings(UserExpressionBase expression)
        {
            this._expression = expression;
        }

        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get { yield return FilterExpressionType.Set; }
        }

        public override Func<TwitterStatus, IReadOnlyCollection<long>> GetSetValueProvider()
        {
            return _ => _expression.Followings;
        }

        public override string ToQuery()
        {
            return _expression.ToQuery() + ".followings"; // "friends" also ok
        }
    }

    public sealed class LocalUserFollowers : ValueBase
    {
        private UserExpressionBase _expression;
        public LocalUserFollowers(UserExpressionBase expression)
        {
            this._expression = expression;
        }

        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get { yield return FilterExpressionType.Set; }
        }

        public override Func<TwitterStatus, IReadOnlyCollection<long>> GetSetValueProvider()
        {
            return _ => _expression.Followers;
        }

        public override string ToQuery()
        {
            return _expression.ToQuery() + ".followers";
        }
    }
}
