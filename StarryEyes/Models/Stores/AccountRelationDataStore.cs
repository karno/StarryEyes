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
        private static object storeLocker = new object();
        private static SortedDictionary<long, AccountData> store = new SortedDictionary<long, AccountData>();

        /// <summary>
        /// Get account data fot the account id.<para />
        /// If not exited, create it new.
        /// </summary>
        /// <param name="id">account id</param>
        /// <returns>account data</returns>
        public static AccountData GetAccountData(long id)
        {
            AccountData data;
            lock (storeLocker)
            {
                if (store.TryGetValue(id, out data))
                    return data;
                data = new AccountData(id);
                store.Add(id, data);
                return data;
            }
        }

        /// <summary>
        /// Get account data for the account.
        /// </summary>
        /// <param name="info">lookup account</param>
        /// <returns>account data</returns>
        public static AccountData GetAccountData(this AuthenticateInfo info)
        {
            return GetAccountData(info.Id);
        }

        /// <summary>
        /// Store data for an account.
        /// </summary>
        /// <param name="data">storing data</param>
        public static void SetAccountData(AccountData data)
        {
            lock (storeLocker)
            {
                store[data.AccountId] = data;
            }
        }

        /// <summary>
        /// Get all existed datas.
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<AccountData> AccountDatas
        {
            get
            {
                lock (storeLocker)
                {
                    return store.Values.ToArray();
                }
            }
        }
    }

    /// <summary>
    /// Store relation info for the account
    /// </summary>
    public class AccountData
    {
        /// <summary>
        /// Integrated event for trigger any AccountData changed.
        /// </summary>
        public static event Action<AccountDataChangedInfo> OnAccountDataUpdated;

        private void RaiseOnAccountDataUpdated(long targetUser, bool isAdded, AccountDataChange change)
        {
            var handler = OnAccountDataUpdated;
            if (handler != null)
            {
                handler(new AccountDataChangedInfo()
                {
                    AccountUserId = this.AccountId,
                    IsIdAdded = isAdded,
                    TargetUserId = targetUser,
                    Change = change
                });
            }
        }

        private long accountId;
        /// <summary>
        /// Bound account Id
        /// </summary>
        public long AccountId
        {
            get { return accountId; }
        }

        /// <summary>
        /// Initialize account data info
        /// </summary>
        /// <param name="accountId">bound account id</param>
        public AccountData(long accountId)
        {
            this.accountId = accountId;
        }

        private object followingsLocker = new object();
        private AVLTree<long> followings = new AVLTree<long>();

        /// <summary>
        /// Get all followings.
        /// </summary>
        public IEnumerable<long> Followings
        {
            get
            {
                lock (followingsLocker)
                {
                    return followings.ToArray();
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
            lock (followingsLocker)
            {
                return followings.Contains(id);
            }
        }

        /// <summary>
        /// Add/remove following user
        /// </summary>
        /// <param name="id">target user's id</param>
        /// <param name="isAdded">flag of follow/remove</param>
        public void SetFollowing(long id, bool isAdded)
        {
            lock (followingsLocker)
            {
                if (isAdded)
                    followings.Add(id);
                else
                    followings.Remove(id);
            }
            RaiseOnAccountDataUpdated(id, isAdded, AccountDataChange.Following);
        }

        /// <summary>
        /// Add following user
        /// </summary>
        /// <param name="id">add user's id</param>
        public void AddFollowing(long id)
        {
            lock (followingsLocker)
            {
                followings.Add(id);
            }
            RaiseOnAccountDataUpdated(id, true, AccountDataChange.Following);
        }

        /// <summary>
        /// Remove following user
        /// </summary>
        /// <param name="id">remove user's id</param>
        public void RemoveFollowing(long id)
        {
            lock (followingsLocker)
            {
                followings.Remove(id);
            }
            RaiseOnAccountDataUpdated(id, false, AccountDataChange.Following);
        }

        private object followersLocker = new object();
        private AVLTree<long> followers = new AVLTree<long>();

        /// <summary>
        /// Get all followers.
        /// </summary>
        public IEnumerable<long> Followers
        {
            get
            {
                lock (followersLocker)
                {
                    return followers.ToArray();
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
            lock (followersLocker)
            {
                return followers.Contains(id);
            }
        }

        /// <summary>
        /// Add/remove follower user
        /// </summary>
        /// <param name="id">target user's id</param>
        /// <param name="isAdded">flag of followed/removed</param>
        public void SetFollower(long id, bool isAdded)
        {
            lock (followersLocker)
            {
                if (isAdded)
                    followers.Add(id);
                else
                    followers.Remove(id);
            }
            RaiseOnAccountDataUpdated(id, isAdded, AccountDataChange.Follower);
        }

        /// <summary>
        /// Add follower user
        /// </summary>
        /// <param name="id">add user's id</param>
        public void AddFollower(long id)
        {
            lock (followersLocker)
            {
                followers.Add(id);
            }
            RaiseOnAccountDataUpdated(id, true, AccountDataChange.Follower);
        }

        /// <summary>
        /// Remove follower user
        /// </summary>
        /// <param name="id">remove user's id</param>
        public void RemoveFollower(long id)
        {
            lock (followersLocker)
            {
                followers.Remove(id);
            }
            RaiseOnAccountDataUpdated(id, false, AccountDataChange.Follower);
        }

        private object blockingsLocker = new object();
        private AVLTree<long> blockings = new AVLTree<long>();

        /// <summary>
        /// Get all blockings
        /// </summary>
        public IEnumerable<long> Blockings
        {
            get
            {
                lock (blockingsLocker)
                {
                    return blockings.ToArray();
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
            lock (blockingsLocker)
            {
                return blockings.Contains(id);
            }
        }

        /// <summary>
        /// Add/remove blocking user
        /// </summary>
        /// <param name="id">target user's id</param>
        /// <param name="isAdded">flag of blocked/unblocked</param>
        public void SetBlocking(long id, bool isAdded)
        {
            lock (blockingsLocker)
            {
                if (isAdded)
                    blockings.Add(id);
                else
                    blockings.Remove(id);
            }
            RaiseOnAccountDataUpdated(id, isAdded, AccountDataChange.Blocking);
        }

        /// <summary>
        /// Add blocking user
        /// </summary>
        /// <param name="id">add user's id</param>
        public void AddBlocking(long id)
        {
            lock (blockingsLocker)
            {
                blockings.Add(id);
            }
            RaiseOnAccountDataUpdated(id, true, AccountDataChange.Blocking);
        }

        /// <summary>
        /// Remove blocking user
        /// </summary>
        /// <param name="id">remove user's id</param>
        public void RemoveBlocking(long id)
        {
            lock (blockingsLocker)
            {
                blockings.Remove(id);
            }
            RaiseOnAccountDataUpdated(id, false, AccountDataChange.Blocking);
        }
    }

    /// <summary>
    /// Information of changing relation
    /// </summary>
    public class AccountDataChangedInfo
    {
        /// <summary>
        /// Change description
        /// </summary>
        public AccountDataChange Change { get; set; }

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
    }

    /// <summary>
    /// Describe changed data
    /// </summary>
    public enum AccountDataChange
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
