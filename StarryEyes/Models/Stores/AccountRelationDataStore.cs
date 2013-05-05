using System;
using System.Collections.Generic;
using System.Linq;
using StarryEyes.Albireo.Data;
using StarryEyes.Breezy.Authorize;

namespace StarryEyes.Models.Stores
{
    /// <summary>
    /// Account data store
    /// </summary>
    public static class AccountRelationDataStore
    {
        private static readonly object StoreLocker = new object();
        private static readonly SortedDictionary<long, AccountRelationData> Store = new SortedDictionary<long, AccountRelationData>();

        public static event Action<RelationDataChangedInfo> AccountDataUpdated;

        private static void OnAccountDataUpdated(RelationDataChangedInfo obj)
        {
            var handler = AccountDataUpdated;
            if (handler != null) handler(obj);
        }

        /// <summary>
        /// Get account data fot the account id.<para />
        /// If not exited, create it new.
        /// </summary>
        /// <param name="id">account id</param>
        /// <returns>account data</returns>
        public static AccountRelationData Get(long id)
        {
            lock (StoreLocker)
            {
                AccountRelationData data;
                if (Store.TryGetValue(id, out data))
                {
                    return data;
                }
                data = new AccountRelationData(id);
                data.AccountDataUpdated += OnAccountDataUpdated;
                Store.Add(id, data);
                return data;
            }
        }

        /// <summary>
        /// Get account data for the account.
        /// </summary>
        /// <param name="info">lookup account</param>
        /// <returns>account data</returns>
        public static AccountRelationData GetRelationData(this AuthenticateInfo info)
        {
            return Get(info.Id);
        }

        /// <summary>
        /// Store data for an account.
        /// </summary>
        /// <param name="data">storing data</param>
        public static void Set(AccountRelationData data)
        {
            lock (StoreLocker)
            {
                Store[data.AccountId] = data;
            }
        }

        /// <summary>
        /// Get all existed datas.
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<AccountRelationData> AccountRelations
        {
            get
            {
                lock (StoreLocker)
                {
                    return Store.Values.ToArray();
                }
            }
        }
    }

    /// <summary>
    /// Store relation info for the account
    /// </summary>
    public class AccountRelationData
    {
        /// <summary>
        /// Integrated event for trigger any AccountRelationData changed.
        /// </summary>
        public event Action<RelationDataChangedInfo> AccountDataUpdated;

        private void RaiseOnAccountDataUpdated(long targetUser, bool isAdded, RelationDataChange change)
        {
            var handler = AccountDataUpdated;
            if (handler != null)
            {
                handler(new RelationDataChangedInfo
                {
                    AccountUserId = this.AccountId,
                    IsIdAdded = isAdded,
                    TargetUserId = targetUser,
                    Change = change
                });
            }
        }

        private readonly long _accountId;
        /// <summary>
        /// Bound account Id
        /// </summary>
        public long AccountId
        {
            get { return _accountId; }
        }

        /// <summary>
        /// Initialize account data info
        /// </summary>
        /// <param name="accountId">bound account id</param>
        public AccountRelationData(long accountId)
        {
            this._accountId = accountId;
        }

        private readonly object _followingLocker = new object();
        private readonly AVLTree<long> _following = new AVLTree<long>();

        /// <summary>
        /// Get all followings.
        /// </summary>
        public IEnumerable<long> Following
        {
            get
            {
                lock (this._followingLocker)
                {
                    return this._following.ToArray();
                }
            }
        }

        /// <summary>
        /// Check is following user(a.k.a. friends)
        /// </summary>
        /// <param name="id">his/her Id</param>
        /// <returns>if true, you have followed him/her.</returns>
        public bool IsFollowing(long id)
        {
            lock (this._followingLocker)
            {
                return this._following.Contains(id);
            }
        }

        /// <summary>
        /// Add/remove following user
        /// </summary>
        /// <param name="id">target user's id</param>
        /// <param name="isAdded">flag of follow/remove</param>
        public void SetFollowing(long id, bool isAdded)
        {
            lock (this._followingLocker)
            {
                if (isAdded)
                    this._following.Add(id);
                else
                    this._following.Remove(id);
            }
            RaiseOnAccountDataUpdated(id, isAdded, RelationDataChange.Following);
        }

        /// <summary>
        /// Add following user
        /// </summary>
        /// <param name="id">add user's id</param>
        public void AddFollowing(long id)
        {
            lock (this._followingLocker)
            {
                this._following.Add(id);
            }
            RaiseOnAccountDataUpdated(id, true, RelationDataChange.Following);
        }

        /// <summary>
        /// Remove following user
        /// </summary>
        /// <param name="id">remove user's id</param>
        public void RemoveFollowing(long id)
        {
            lock (this._followingLocker)
            {
                this._following.Remove(id);
            }
            RaiseOnAccountDataUpdated(id, false, RelationDataChange.Following);
        }

        private readonly object _followersLocker = new object();
        private readonly AVLTree<long> _followers = new AVLTree<long>();

        /// <summary>
        /// Get all followers.
        /// </summary>
        public IEnumerable<long> Followers
        {
            get
            {
                lock (_followersLocker)
                {
                    return _followers.ToArray();
                }
            }
        }

        /// <summary>
        /// Check is followed user(a.k.a. follower)
        /// </summary>
        /// <param name="id">his/her id</param>
        /// <returns>if true, you are followed by him/her.</returns>
        public bool IsFollowedBy(long id)
        {
            lock (_followersLocker)
            {
                return _followers.Contains(id);
            }
        }

        /// <summary>
        /// Add/remove follower user
        /// </summary>
        /// <param name="id">target user's id</param>
        /// <param name="isAdded">flag of followed/removed</param>
        public void SetFollower(long id, bool isAdded)
        {
            lock (_followersLocker)
            {
                if (isAdded)
                    _followers.Add(id);
                else
                    _followers.Remove(id);
            }
            RaiseOnAccountDataUpdated(id, isAdded, RelationDataChange.Follower);
        }

        /// <summary>
        /// Add follower user
        /// </summary>
        /// <param name="id">add user's id</param>
        public void AddFollower(long id)
        {
            lock (_followersLocker)
            {
                _followers.Add(id);
            }
            RaiseOnAccountDataUpdated(id, true, RelationDataChange.Follower);
        }

        /// <summary>
        /// Remove follower user
        /// </summary>
        /// <param name="id">remove user's id</param>
        public void RemoveFollower(long id)
        {
            lock (_followersLocker)
            {
                _followers.Remove(id);
            }
            RaiseOnAccountDataUpdated(id, false, RelationDataChange.Follower);
        }

        private readonly object _blockingsLocker = new object();
        private readonly AVLTree<long> _blockings = new AVLTree<long>();

        /// <summary>
        /// Get all blockings
        /// </summary>
        public IEnumerable<long> Blockings
        {
            get
            {
                lock (_blockingsLocker)
                {
                    return _blockings.ToArray();
                }
            }
        }

        /// <summary>
        /// Check someone is blocked me
        /// </summary>
        /// <param name="id">his/her id</param>
        /// <returns>if true, he/she has been blocked me.</returns>
        public bool IsBlocking(long id)
        {
            lock (_blockingsLocker)
            {
                return _blockings.Contains(id);
            }
        }

        /// <summary>
        /// Add/remove blocking user
        /// </summary>
        /// <param name="id">target user's id</param>
        /// <param name="isAdded">flag of blocked/unblocked</param>
        public void SetBlocking(long id, bool isAdded)
        {
            lock (_blockingsLocker)
            {
                if (isAdded)
                    _blockings.Add(id);
                else
                    _blockings.Remove(id);
            }
            RaiseOnAccountDataUpdated(id, isAdded, RelationDataChange.Blocking);
        }

        /// <summary>
        /// Add blocking user
        /// </summary>
        /// <param name="id">add user's id</param>
        public void AddBlocking(long id)
        {
            lock (_blockingsLocker)
            {
                _blockings.Add(id);
            }
            RaiseOnAccountDataUpdated(id, true, RelationDataChange.Blocking);
        }

        /// <summary>
        /// Remove blocking user
        /// </summary>
        /// <param name="id">remove user's id</param>
        public void RemoveBlocking(long id)
        {
            lock (_blockingsLocker)
            {
                _blockings.Remove(id);
            }
            RaiseOnAccountDataUpdated(id, false, RelationDataChange.Blocking);
        }
    }

    /// <summary>
    /// Information of changing relation
    /// </summary>
    public class RelationDataChangedInfo
    {
        /// <summary>
        /// Change description
        /// </summary>
        public RelationDataChange Change { get; set; }

        /// <summary>
        /// Flag of user is added or removed
        /// </summary>
        public bool IsIdAdded { get; set; }

        /// <summary>
        /// target user's id
        /// </summary>
        public long TargetUserId { get; set; }

        /// <summary>
        /// Acted account user's id
        /// </summary>
        public long AccountUserId { get; set; }

        public override string ToString()
        {
            return Change.ToString() + " " + AccountUserId + " => " + TargetUserId;
        }
    }

    /// <summary>
    /// Describe changed data
    /// </summary>
    public enum RelationDataChange
    {
        /// <summary>
        /// Following users is updated
        /// </summary>
        Following,
        /// <summary>
        /// Follower users is updated
        /// </summary>
        Follower,
        /// <summary>
        /// Blocking users is updated
        /// </summary>
        Blocking,
    }
}
