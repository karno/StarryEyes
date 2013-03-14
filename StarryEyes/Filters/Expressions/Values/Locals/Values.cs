using System;
using System.Collections.Generic;
using StarryEyes.Breezy.DataModel;

namespace StarryEyes.Filters.Expressions.Values.Locals
{
    public sealed class LocalUser : ValueBase
    {
        private readonly UserExpressionBase _expression;
        private readonly IReadOnlyCollection<long> _setCache;

        public LocalUser(UserExpressionBase expression)
        {
            this._expression = expression;
            _setCache = _expression.Users;
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
            return base.GetNumericValueProvider();
        }

        public override Func<TwitterStatus, IReadOnlyCollection<long>> GetSetValueProvider()
        {
            return _ => _setCache;
        }

        public override string ToQuery()
        {
            return _expression.ToQuery();
        }
    }

    public sealed class LocalUserFollowings : ValueBase
    {
        private readonly UserExpressionBase _expression;
        private readonly IReadOnlyCollection<long> _setCache;

        public LocalUserFollowings(UserExpressionBase expression)
        {
            this._expression = expression;
            _setCache = _expression.Followings;
        }

        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get { yield return FilterExpressionType.Set; }
        }

        public override Func<TwitterStatus, IReadOnlyCollection<long>> GetSetValueProvider()
        {
            return _ => _setCache;
        }

        public override string ToQuery()
        {
            return _expression.ToQuery() + ".followings"; // "friends" also ok
        }
    }

    public sealed class LocalUserFollowers : ValueBase
    {
        private readonly UserExpressionBase _expression;
        private readonly IReadOnlyCollection<long> _setCache;

        public LocalUserFollowers(UserExpressionBase expression)
        {
            this._expression = expression;
            _setCache = _expression.Followers;
        }

        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get { yield return FilterExpressionType.Set; }
        }

        public override Func<TwitterStatus, IReadOnlyCollection<long>> GetSetValueProvider()
        {
            return _ => _setCache;
        }

        public override string ToQuery()
        {
            return _expression.ToQuery() + ".followers";
        }
    }
}
