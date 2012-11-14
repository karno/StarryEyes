using System;
using System.Collections.Generic;
using System.Linq;
using StarryEyes.Albireo.Data;
using StarryEyes.Models.Stores;
using StarryEyes.Settings;

namespace StarryEyes.Filters.Expressions.Values.Locals
{
    public sealed class UserSpecified : UserRepresentationBase
    {
        string _originalScreenName;
        long _userId;
        AccountData _adata;

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
            _adata = AccountRelationDataStore.GetAccountData(_userId);
        }

        public override IReadOnlyCollection<long> Users
        {
            get
            {
                if (_adata == null)
                    return new List<long>();
                else
                    return new List<long>(new[] { _adata.AccountId });
            }
        }

        public override IReadOnlyCollection<long> Followings
        {
            get
            {
                if (_adata == null)
                    return new List<long>(); // returns empty list
                else
                    return new AVLTree<long>(_adata.Followings);
            }
        }

        public override IReadOnlyCollection<long> Followers
        {
            get
            {
                if (_adata == null)
                    return new List<long>();
                else
                    return new AVLTree<long>(_adata.Followers);
            }
        }

        public override string ToQuery()
        {
            if (String.IsNullOrEmpty(_originalScreenName))
                return "local.#" + _userId.ToString();
            else
                return "local." + _originalScreenName;
        }

        public override long UserId
        {
            get { return _userId; }
        }
    }
}
