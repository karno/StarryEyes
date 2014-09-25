using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using StarryEyes.Albireo.Collections;
using StarryEyes.Models.Accounting;
using StarryEyes.Models.Databases;
using StarryEyes.Settings;

namespace StarryEyes.Filters.Expressions.Values.Locals
{
    public sealed class UserSpecified : UserExpressionBase
    {
        readonly string _originalScreenName;
        readonly long _userId;
        readonly AccountRelationData _adata;

        public UserSpecified(string screenName)
        {
            _originalScreenName = screenName;
            _userId = Setting.Accounts
                             .Collection
                             .Where(u => u.UnreliableScreenName.Equals(
                                 screenName, StringComparison.CurrentCultureIgnoreCase))
                             .Select(u => u.Id)
                             .FirstOrDefault();
            if (_userId == 0)
            {
                _userId = UserProxy.GetId(screenName);
            }
            else
            {
                var account = Setting.Accounts.Get(_userId);
                _adata = account != null ? account.RelationData : null;
            }
        }

        public UserSpecified(long id)
        {
            _userId = id;
            var account = Setting.Accounts.Get(_userId);
            _adata = account != null ? account.RelationData : null;
        }

        public override void BeginLifecycle()
        {
            AccountRelationData.AccountDataUpdatedStatic += this.AccountRelationDataAccountDataUpdated;
        }

        public override void EndLifecycle()
        {
            AccountRelationData.AccountDataUpdatedStatic -= this.AccountRelationDataAccountDataUpdated;
        }

        void AccountRelationDataAccountDataUpdated(RelationDataChangedInfo obj)
        {
            if (obj.AccountUserId == _userId)
            {
                this.RaiseReapplyFilter(obj);
            }
        }

        public override long UserId
        {
            get { return _userId; }
        }

        public override IReadOnlyCollection<long> Users
        {
            get
            {
                return new List<long>(new[] { _userId });
            }
        }

        public override IReadOnlyCollection<long> Followings
        {
            get
            {
                if (_adata == null)
                    return new List<long>(); // returns empty list
                return new AVLTree<long>(_adata.Followings.Items);
            }
        }

        public override IReadOnlyCollection<long> Followers
        {
            get
            {
                if (_adata == null)
                    return new List<long>();
                return new AVLTree<long>(_adata.Followers.Items);
            }
        }

        public override IReadOnlyCollection<long> Blockings
        {
            get
            {
                if (_adata == null)
                    return new List<long>();
                return new AVLTree<long>(_adata.Blockings.Items);
            }
        }

        public override IReadOnlyCollection<long> Mutes
        {
            get
            {
                if (_adata == null)
                    return new List<long>();
                return new AVLTree<long>(_adata.Mutes.Items);
            }
        }

        public override string UserIdSql
        {
            get { return _userId.ToString(CultureInfo.InvariantCulture); }
        }

        public override string UsersSql
        {
            get { return "(select Id from Accounts where Id = " + UserId + ")"; }
        }

        public override string FollowingsSql
        {
            get { return "(select TargetId from Followings where UserId = " + UserId + ")"; }
        }

        public override string FollowersSql
        {
            get { return "(select TargetId from Followers where UserId = " + UserId + ")"; }
        }

        public override string BlockingsSql
        {
            get { return "(select TargetId from Blockings where UserId = " + UserId + ")"; }
        }

        public override string MutesSql
        {
            get { return "(select TargetId from Mutes where UserId = " + UserId + ")"; }
        }

        public override string ToQuery()
        {
            return String.IsNullOrEmpty(this._originalScreenName)
                ? "#" + this._userId.ToString(CultureInfo.InvariantCulture)
                : "@" + this._originalScreenName;
        }
    }
}
