using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using StarryEyes.Albireo.Data;
using StarryEyes.Models.Stores;

namespace StarryEyes.Filters.Expressions.Values.Locals
{
    public sealed class UserSpecified : UserExpressionBase
    {
        readonly string _originalScreenName;
        readonly long _userId;
        AccountRelationData _adata;

        public UserSpecified(string screenName)
        {
            _originalScreenName = screenName;
            _userId = AccountsStore.Accounts
                .Where(u => u.AuthenticateInfo.UnreliableScreenName == screenName)
                .Select(u => u.UserId)
                .FirstOrDefault();
            GetAccountData();
        }

        public UserSpecified(long id)
        {
            _userId = id;
            GetAccountData();
        }

        private void GetAccountData()
        {
            _adata = AccountRelationDataStore.Get(_userId);
        }

        public override IReadOnlyCollection<long> Users
        {
            get
            {
                if (_adata == null)
                    return new List<long>();
                return new List<long>(new[] { _adata.AccountId });
            }
        }

        public override IReadOnlyCollection<long> Followings
        {
            get
            {
                if (_adata == null)
                    return new List<long>(); // returns empty list
                return new AVLTree<long>(_adata.Followings);
            }
        }

        public override IReadOnlyCollection<long> Followers
        {
            get
            {
                if (_adata == null)
                    return new List<long>();
                return new AVLTree<long>(_adata.Followers);
            }
        }

        public override string ToQuery()
        {
            if (String.IsNullOrEmpty(_originalScreenName))
                return "#" + _userId.ToString(CultureInfo.InvariantCulture);
            return "@" + _originalScreenName;
        }

        public override long UserId
        {
            get { return _userId; }
        }
    }
}
