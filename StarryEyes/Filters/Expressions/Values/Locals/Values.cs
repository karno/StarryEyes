using System;
using System.Collections.Generic;
using StarryEyes.Breezy.DataModel;
using StarryEyes.Models.Stores;

namespace StarryEyes.Filters.Expressions.Values.Locals
{
    public sealed class LocalUser : ValueBase
    {
        private readonly UserExpressionBase _expression;

        public LocalUser(UserExpressionBase expression)
        {
            this._expression = expression;
            this._expression.OnReapplyRequested += _expression_OnReapplyRequested;
        }

        private void _expression_OnReapplyRequested(RelationDataChangedInfo obj)
        {
            if (obj != null) return;
            System.Diagnostics.Debug.WriteLine("local user reapply");
            RequestReapplyFilter();
        }

        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get
            {
                if (_expression.UserId != -1)
                    yield return FilterExpressionType.Numeric;
                yield return FilterExpressionType.Set;
            }
        }

        public override Func<TwitterStatus, long> GetNumericValueProvider()
        {
            if (_expression.UserId != -1)
                return _ => _expression.UserId;
            return base.GetNumericValueProvider();
        }

        public override Func<TwitterStatus, IReadOnlyCollection<long>> GetSetValueProvider()
        {
            var cache = _expression.Users;
            return _ => cache;
        }

        public override void BeginLifecycle()
        {
            _expression.BeginLifecycle();
        }

        public override void EndLifecycle()
        {
            _expression.EndLifecycle();
        }

        public override string ToQuery()
        {
            return _expression.ToQuery();
        }
    }

    public sealed class LocalUserFollowings : ValueBase
    {
        private readonly UserExpressionBase _expression;

        public LocalUserFollowings(UserExpressionBase expression)
        {
            this._expression = expression;
            this._expression.OnReapplyRequested += _expression_OnReapplyRequested;
        }

        private void _expression_OnReapplyRequested(RelationDataChangedInfo obj)
        {
            if (obj == null || obj.Change == RelationDataChange.Following)
            {
                RequestReapplyFilter();
            }
        }

        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get { yield return FilterExpressionType.Set; }
        }

        public override Func<TwitterStatus, IReadOnlyCollection<long>> GetSetValueProvider()
        {
            var cache = _expression.Followings;
            return _ => cache;
        }

        public override void BeginLifecycle()
        {
            _expression.BeginLifecycle();
        }

        public override void EndLifecycle()
        {
            _expression.EndLifecycle();
        }

        public override string ToQuery()
        {
            return _expression.ToQuery() + ".followings"; // "friends" also ok
        }
    }

    public sealed class LocalUserFollowers : ValueBase
    {
        private readonly UserExpressionBase _expression;

        public LocalUserFollowers(UserExpressionBase expression)
        {
            this._expression = expression;
            this._expression.OnReapplyRequested += _expression_OnReapplyRequested;
        }

        private void _expression_OnReapplyRequested(RelationDataChangedInfo obj)
        {
            if (obj == null || obj.Change == RelationDataChange.Follower)
            {
                RequestReapplyFilter();
            }
        }

        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get { yield return FilterExpressionType.Set; }
        }

        public override Func<TwitterStatus, IReadOnlyCollection<long>> GetSetValueProvider()
        {
            var cache = _expression.Followers;
            return _ => cache;
        }

        public override void BeginLifecycle()
        {
            _expression.BeginLifecycle();
        }

        public override void EndLifecycle()
        {
            _expression.EndLifecycle();
        }

        public override string ToQuery()
        {
            return _expression.ToQuery() + ".followers";
        }
    }

    public sealed class LocalUserBlockings : ValueBase
    {
        private readonly UserExpressionBase _expression;

        public LocalUserBlockings(UserExpressionBase expression)
        {
            _expression = expression;
            this._expression.OnReapplyRequested += _expression_OnReapplyRequested;
        }

        private void _expression_OnReapplyRequested(RelationDataChangedInfo obj)
        {
            if (obj == null || obj.Change == RelationDataChange.Blocking)
            {
                RequestReapplyFilter();
            }
        }

        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get { yield return FilterExpressionType.Set; }
        }

        public override Func<TwitterStatus, IReadOnlyCollection<long>> GetSetValueProvider()
        {
            var cache = _expression.Blockings;
            return _ => cache;
        }

        public override void BeginLifecycle()
        {
            _expression.BeginLifecycle();
        }

        public override void EndLifecycle()
        {
            _expression.EndLifecycle();
        }

        public override string ToQuery()
        {
            return _expression.ToQuery() + ".blockings";
        }
    }
}
