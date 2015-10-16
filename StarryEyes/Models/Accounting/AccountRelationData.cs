using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StarryEyes.Albireo.Collections;
using StarryEyes.Albireo.Helpers;
using StarryEyes.Models.Databases;

namespace StarryEyes.Models.Accounting
{
    /// <summary>
    /// Store relation info for the account
    /// </summary>
    public class AccountRelationData
    {
        public static event Action<RelationDataChangedInfo> AccountDataUpdatedStatic;

        private readonly AccountRelationDataChunk _followings;
        /// <summary>
        /// Following info
        /// </summary>
        public AccountRelationDataChunk Followings
        {
            get { return _followings; }
        }

        private readonly AccountRelationDataChunk _followers;
        /// <summary>
        /// Follower info
        /// </summary>
        public AccountRelationDataChunk Followers
        {
            get { return _followers; }
        }

        private readonly AccountRelationDataChunk _blockings;
        /// <summary>
        /// Blocking info
        /// </summary>
        public AccountRelationDataChunk Blockings
        {
            get { return _blockings; }
        }

        private readonly AccountRelationDataChunk _noRetweets;
        /// <summary>
        /// No Retweets info
        /// </summary>
        public AccountRelationDataChunk NoRetweets
        {
            get { return _noRetweets; }
        }

        private readonly AccountRelationDataChunk _mutes;
        /// <summary>
        /// Mutes info
        /// </summary>
        public AccountRelationDataChunk Mutes
        {
            get { return _mutes; }
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
        /// Integrated event for trigger any AccountRelationData changed.
        /// </summary>
        public event Action<RelationDataChangedInfo> AccountDataUpdated;

        /// <summary>
        /// Initialize account data info
        /// </summary>
        /// <param name="accountId">bound account id</param>
        public AccountRelationData(long accountId)
        {
            _accountId = accountId;
            _followings = new AccountRelationDataChunk(this, RelationDataType.Following);
            _followers = new AccountRelationDataChunk(this, RelationDataType.Follower);
            _blockings = new AccountRelationDataChunk(this, RelationDataType.Blocking);
            _noRetweets = new AccountRelationDataChunk(this, RelationDataType.NoRetweets);
            _mutes = new AccountRelationDataChunk(this, RelationDataType.Mutes);
            _followings.AccountDataUpdated += PropagateEvent;
            _followers.AccountDataUpdated += PropagateEvent;
            _blockings.AccountDataUpdated += PropagateEvent;
            NoRetweets.AccountDataUpdated += PropagateEvent;
            _mutes.AccountDataUpdated += PropagateEvent;
        }

        private void PropagateEvent(RelationDataChangedInfo e)
        {
            AccountDataUpdated.SafeInvoke(e);
            AccountDataUpdatedStatic.SafeInvoke(e);
        }
    }

    public class AccountRelationDataChunk
    {
        private readonly AccountRelationData _parent;
        private readonly RelationDataType _type;

        private readonly object _collectionLock = new object();
        private readonly AVLTree<long> _collection = new AVLTree<long>();

        /// <summary>
        /// Integrated event for trigger any AccountRelationData changed.
        /// </summary>
        public event Action<RelationDataChangedInfo> AccountDataUpdated;

        private void RaiseAccountDataUpdated(IEnumerable<long> targetUsers, bool isAdded)
        {
            var rdci = new RelationDataChangedInfo
            {
                AccountUserId = _parent.AccountId,
                IsIdAdded = isAdded,
                TargetUserIds = targetUsers,
                Type = _type
            };
            AccountDataUpdated.SafeInvoke(rdci);
        }

        public AccountRelationDataChunk(AccountRelationData parent, RelationDataType type)
        {
            _parent = parent;
            _type = type;
            Task.Run(() => InitializeCollection());
        }

        private async Task InitializeCollection()
        {
            Task<IEnumerable<long>> reader;
            switch (_type)
            {
                case RelationDataType.Following:
                    reader = UserProxy.GetFollowingsAsync(_parent.AccountId);
                    break;
                case RelationDataType.Follower:
                    reader = UserProxy.GetFollowersAsync(_parent.AccountId);
                    break;
                case RelationDataType.Blocking:
                    reader = UserProxy.GetBlockingsAsync(_parent.AccountId);
                    break;
                case RelationDataType.NoRetweets:
                    reader = UserProxy.GetNoRetweetsAsync(_parent.AccountId);
                    break;
                case RelationDataType.Mutes:
                    reader = UserProxy.GetMutesAsync(_parent.AccountId);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            await AddAsync(await reader.ConfigureAwait(false)).ConfigureAwait(false);
        }

        public IEnumerable<long> Items
        {
            get
            {
                lock (_collectionLock)
                {
                    return _collection.ToArray();
                }
            }
        }

        /// <summary>
        /// Check contains specified user.
        /// </summary>
        /// <param name="id">user id</param>
        /// <returns>if contains specified user, return true.</returns>
        public bool Contains(long id)
        {
            lock (_collectionLock)
            {
                return _collection.Contains(id);
            }
        }

        /// <summary>
        /// Add/remove user id in this container.
        /// </summary>
        /// <param name="id">target user's id</param>
        /// <param name="value">flag for add or remove</param>
        public async Task SetAsync(long id, bool value)
        {
            lock (_collectionLock)
            {
                var result = value ? _collection.AddDistinct(id) : _collection.Remove(id);
                if (!result)
                {
                    // not changed
                    return;
                }
            }
            await UserProxy.SetAsync(_type, _parent.AccountId, id, value).ConfigureAwait(false);
            RaiseAccountDataUpdated(new[] { id }, value);
        }

        /// <summary>
        /// Overwrite all elements by specified ids.
        /// </summary>
        /// <param name="ids">new ids</param>
        public async Task SetAsync(IEnumerable<long> ids)
        {
            var items = ids.ToArray();
            long[] currents;
            lock (_collectionLock)
            {
                currents = _collection.ToArray();
            }
            var news = items.Except(currents).ToArray();
            var olds = currents.Except(items).ToArray();
            await RemoveAsync(olds).ConfigureAwait(false);
            await AddAsync(news).ConfigureAwait(false);
        }

        private async Task AddAsync(IEnumerable<long> items)
        {
            var m = items.ToArray();
            lock (_collectionLock)
            {
                m = m.Where(i => _collection.AddDistinct(i)).ToArray();
            }
            if (m.Length == 0) return;
            await UserProxy.AddAsync(_type, _parent.AccountId, m).ConfigureAwait(false);
            RaiseAccountDataUpdated(m, true);
        }

        private async Task RemoveAsync(IEnumerable<long> items)
        {
            var m = items.ToArray();
            lock (_collectionLock)
            {
                m = m.Where(i => _collection.Remove(i)).ToArray();
            }
            if (m.Length == 0) return;
            await UserProxy.RemoveAsync(_type, _parent.AccountId, m).ConfigureAwait(false);
            RaiseAccountDataUpdated(m, false);
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
        public RelationDataType Type { get; set; }

        /// <summary>
        /// Flag of user is added or removed
        /// </summary>
        public bool IsIdAdded { get; set; }

        /// <summary>
        /// target user's id collection
        /// </summary>
        public IEnumerable<long> TargetUserIds { get; set; }

        /// <summary>
        /// Acted account user's id
        /// </summary>
        public long AccountUserId { get; set; }

        public override string ToString()
        {
            return Type + " " + AccountUserId + " => " +
                   TargetUserIds.Select(s => s.ToString()).JoinString(", ");
        }
    }

}