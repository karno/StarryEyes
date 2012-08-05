using System;
using System.Collections.Generic;
using System.Linq;
using StarryEyes.Albireo.Data;

namespace StarryEyes.Mystique.Models.Common
{
    public class AccountData
    {
        private long accountId;
        public long AccountId
        {
            get { return accountId; }
        }

        public AccountData(long accountId)
        {
            this.accountId = accountId;
            int i;
        }

        private object followingsLocker = new object();
        private AVLTree<long> followings = new AVLTree<long>();

        public bool IsFollowing(long id)
        {
            lock (followingsLocker)
            {
                return followings.Contains(id);
            }
        }

        public void AddFollowing(long id)
        {
            lock (followingsLocker)
            {
                followings.Add(id);
            }
        }

        public void RemoveFollowing(long id)
        {
            lock (followingsLocker)
            {
                followings.Remove(id);
            }
        }

        private object followersLocker = new object();
        private AVLTree<long> followers = new AVLTree<long>();

        public bool IsFollowedBy(long id)
        {
            lock (followersLocker)
            {
                return followers.Contains(id);
            }
        }

        public void AddFollower(long id)
        {
            lock (followersLocker)
            {
                followers.Add(id);
            }
        }

        public void RemoveFollower(long id)
        {
            lock (followersLocker)
            {
                followers.Remove(id);
            }
        }
    }
}
