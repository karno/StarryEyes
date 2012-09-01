using System;
using System.Collections.Generic;
using System.Linq;
using StarryEyes.Mystique.Models.Common;
using StarryEyes.Mystique.Models.Store;
using StarryEyes.Mystique.Settings;

namespace StarryEyes.Mystique.Filters.Expressions.Values.Locals
{
    public sealed class UserSpecified : UserRepresentationBase
    {
        string _originalScreenName;
        long _userId;
        AccountData _adata;

        public UserSpecified(string screenName)
        {
            _originalScreenName = screenName;
            _userId = Setting.Accounts
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
            _adata = AccountDataStore.GetAccountData(_userId);
        }

        public override ICollection<long> Users
        {
            get
            {
                if (_adata == null)
                    return new List<long>();
                else
                    return new PseudoCollection<long>(_ => _adata.AccountId == _);
            }
        }

        public override ICollection<long> Followings
        {
            get
            {
                if (_adata == null)
                    return new List<long>(); // returns empty list
                else
                    return new PseudoCollection<long>(_ => _adata.IsFollowing(_));
            }
        }

        public override ICollection<long> Followers
        {
            get
            {
                if (_adata == null)
                    return new List<long>();
                else
                    return new PseudoCollection<long>(_ => _adata.IsFollowedBy(_));
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
