using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StarryEyes.Albireo.Data;
using StarryEyes.Models.Databases;

namespace StarryEyes.Models.Accounting
{
    /// <summary>
    /// Store relation info for the account
    /// </summary>
    public class AccountRelationData
    {
        public static event Action<RelationDataChangedInfo> AccountDataUpdatedStatic;

        private static void OnAccountDataUpdatedStatic(RelationDataChangedInfo obj)
        {
            var handler = AccountDataUpdatedStatic;
            if (handler != null) handler(obj);
        }

        /// <summary>
        /// Integrated event for trigger any AccountRelationData changed.
        /// </summary>
        public event Action<RelationDataChangedInfo> AccountDataUpdated;

        private void OnAccountDataUpdated(long targetUser, bool isAdded, RelationDataChange change)
        {
            var rdci = new RelationDataChangedInfo
            {
                AccountUserId = this.AccountId,
                IsIdAdded = isAdded,
                TargetUserId = targetUser,
                Change = change
            };
            var handler = this.AccountDataUpdated;
            if (handler != null)
            {
                handler(rdci);
            }
            OnAccountDataUpdatedStatic(rdci);
        }

        private readonly long _accountId;
        /// <summary>
        /// Bound account Id
        /// </summary>
        public long AccountId
        {
            get { return this._accountId; }
        }

        /// <summary>
        /// Initialize account data info
        /// </summary>
        /// <param name="accountId">bound account id</param>
        public AccountRelationData(long accountId)
        {
            this._accountId = accountId;
            // load data from db
            Task.Run(async () =>
                     (await UserProxy.GetFollowingsAsync(accountId))
                         .ForEach(this._followings.Add));
            Task.Run(async () =>
                     (await UserProxy.GetFollowersAsync(accountId))
                         .ForEach(this._followers.Add));
            Task.Run(async () =>
                     (await UserProxy.GetBlockingsAsync(accountId))
                         .ForEach(this._blockings.Add));
        }

        private readonly object _followingLocker = new object();
        private readonly AVLTree<long> _followings = new AVLTree<long>();

        /// <summary>
        /// Get all followings.
        /// </summary>
        public IEnumerable<long> Followings
        {
            get
            {
                lock (this._followingLocker)
                {
                    return this._followings.ToArray();
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
                return this._followings.Contains(id);
            }
        }

        /// <summary>
        /// Add/remove following user
        /// </summary>
        /// <param name="id">target user's id</param>
        /// <param name="isAdded">flag of follow/remove</param>
        public async Task SetFollowingAsync(long id, bool isAdded)
        {
            lock (this._followingLocker)
            {
                if (isAdded)
                {
                    this._followings.Add(id);
                }
                else
                {
                    this._followings.Remove(id);
                }
            }
            await UserProxy.SetFollowingAsync(_accountId, id, isAdded);
            this.OnAccountDataUpdated(id, isAdded, RelationDataChange.Following);
        }

        /// <summary>
        /// Add following users
        /// </summary>
        /// <param name="ids">target users' ids</param>
        public async Task AddFollowingsAsync(IEnumerable<long> ids)
        {
            var m = ids.Memoize();
            lock (this._followingLocker)
            {
                m.ForEach(this._followings.Add);
            }
            await UserProxy.AddFollowingsAsync(_accountId, m);
            m.ForEach(id => this.OnAccountDataUpdated(id, true, RelationDataChange.Following));
        }

        /// <summary>
        /// Remove following users
        /// </summary>
        /// <param name="ids">target users' ids</param>
        public async Task RemoveFollowingsAsync(IEnumerable<long> ids)
        {
            var m = ids.Memoize();
            lock (this._followingLocker)
            {
                m.ForEach(i => this._followings.Remove(i));
            }
            await UserProxy.RemoveFollowingsAsync(_accountId, m);
            m.ForEach(id => this.OnAccountDataUpdated(id, false, RelationDataChange.Following));
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
                lock (this._followersLocker)
                {
                    return this._followers.ToArray();
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
            lock (this._followersLocker)
            {
                return this._followers.Contains(id);
            }
        }

        /// <summary>
        /// Add/remove follower user
        /// </summary>
        /// <param name="id">target user's id</param>
        /// <param name="isAdded">flag of followed/removed</param>
        public async Task SetFollowerAsync(long id, bool isAdded)
        {
            lock (this._followersLocker)
            {
                if (isAdded)
                {
                    this._followers.Add(id);
                }
                else
                {
                    this._followers.Remove(id);
                }
            }
            await UserProxy.SetFollowerAsync(_accountId, id, isAdded);
            this.OnAccountDataUpdated(id, isAdded, RelationDataChange.Follower);
        }

        /// <summary>
        /// Add follower users
        /// </summary>
        /// <param name="ids">target users' ids</param>
        public async Task AddFollowersAsync(IEnumerable<long> ids)
        {
            var m = ids.Memoize();
            lock (this._followersLocker)
            {
                m.ForEach(this._followers.Add);
            }
            await UserProxy.AddFollowersAsync(_accountId, m);
            m.ForEach(id => this.OnAccountDataUpdated(id, true, RelationDataChange.Follower));
        }

        /// <summary>
        /// Remove follower users
        /// </summary>
        /// <param name="ids">target users' ids</param>
        public async Task RemoveFollowersAsync(IEnumerable<long> ids)
        {
            var m = ids.Memoize();
            lock (this._followersLocker)
            {
                m.ForEach(i => this._followers.Remove(i));
            }
            await UserProxy.RemoveFollowersAsync(_accountId, m);
            m.ForEach(id => this.OnAccountDataUpdated(id, false, RelationDataChange.Follower));
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
                lock (this._blockingsLocker)
                {
                    return this._blockings.ToArray();
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
            lock (this._blockingsLocker)
            {
                return this._blockings.Contains(id);
            }
        }

        /// <summary>
        /// Add/remove blocking user
        /// </summary>
        /// <param name="id">target user's id</param>
        /// <param name="isAdded">flag of blocked/unblocked</param>
        public async Task SetBlockingAsync(long id, bool isAdded)
        {
            lock (this._blockingsLocker)
            {
                if (isAdded)
                {
                    this._blockings.Add(id);
                }
                else
                {
                    this._blockings.Remove(id);
                }
            }
            await UserProxy.SetBlockingAsync(_accountId, id, isAdded);
            this.OnAccountDataUpdated(id, isAdded, RelationDataChange.Blocking);
        }

        /// <summary>
        /// Add blocking users
        /// </summary>
        /// <param name="ids">target users' ids</param>
        public async Task AddBlockingsAsync(IEnumerable<long> ids)
        {
            var m = ids.Memoize();
            lock (this._blockingsLocker)
            {
                m.ForEach(this._blockings.Add);
            }
            await UserProxy.AddBlockingsAsync(_accountId, m);
            m.ForEach(id => this.OnAccountDataUpdated(id, true, RelationDataChange.Blocking));
        }

        /// <summary>
        /// Remove blocking users
        /// </summary>
        /// <param name="ids">target users' ids</param>
        public async Task RemoveBlockingsAsync(IEnumerable<long> ids)
        {
            var m = ids.Memoize();
            lock (this._blockingsLocker)
            {
                m.ForEach(i => this._blockings.Remove(i));
            }
            await UserProxy.RemoveBlockingsAsync(_accountId, m);
            m.ForEach(id => this.OnAccountDataUpdated(id, false, RelationDataChange.Blocking));
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
            return this.Change.ToString() + " " + this.AccountUserId + " => " + this.TargetUserId;
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