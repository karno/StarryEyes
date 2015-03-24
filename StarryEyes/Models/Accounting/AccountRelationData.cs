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
            get { return this._followings; }
        }

        private readonly AccountRelationDataChunk _followers;
        /// <summary>
        /// Follower info
        /// </summary>
        public AccountRelationDataChunk Followers
        {
            get { return this._followers; }
        }

        private readonly AccountRelationDataChunk _blockings;
        /// <summary>
        /// Blocking info
        /// </summary>
        public AccountRelationDataChunk Blockings
        {
            get { return this._blockings; }
        }

        private readonly AccountRelationDataChunk _noRetweets;
        /// <summary>
        /// No Retweets info
        /// </summary>
        public AccountRelationDataChunk NoRetweets
        {
            get { return this._noRetweets; }
        }

        private readonly AccountRelationDataChunk _mutes;
        /// <summary>
        /// Mutes info
        /// </summary>
        public AccountRelationDataChunk Mutes
        {
            get { return this._mutes; }
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
        /// Integrated event for trigger any AccountRelationData changed.
        /// </summary>
        public event Action<RelationDataChangedInfo> AccountDataUpdated;

        /// <summary>
        /// Initialize account data info
        /// </summary>
        /// <param name="accountId">bound account id</param>
        public AccountRelationData(long accountId)
        {
            this._accountId = accountId;
            this._followings = new AccountRelationDataChunk(this, RelationDataType.Following);
            this._followers = new AccountRelationDataChunk(this, RelationDataType.Follower);
            this._blockings = new AccountRelationDataChunk(this, RelationDataType.Blocking);
            this._noRetweets = new AccountRelationDataChunk(this, RelationDataType.NoRetweets);
            this._mutes = new AccountRelationDataChunk(this, RelationDataType.Mutes);
            this._followings.AccountDataUpdated += this.PropagateEvent;
            this._followers.AccountDataUpdated += this.PropagateEvent;
            this._blockings.AccountDataUpdated += this.PropagateEvent;
            this.NoRetweets.AccountDataUpdated += this.PropagateEvent;
            this._mutes.AccountDataUpdated += this.PropagateEvent;
        }

        private void PropagateEvent(RelationDataChangedInfo e)
        {
            this.AccountDataUpdated.SafeInvoke(e);
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
                AccountUserId = this._parent.AccountId,
                IsIdAdded = isAdded,
                TargetUserIds = targetUsers,
                Type = _type
            };
            this.AccountDataUpdated.SafeInvoke(rdci);
        }

        public AccountRelationDataChunk(AccountRelationData parent, RelationDataType type)
        {
            this._parent = parent;
            this._type = type;
            Task.Run(() => this.InitializeCollection());
        }

        private async Task InitializeCollection()
        {
            Task<IEnumerable<long>> reader;
            switch (_type)
            {
                case RelationDataType.Following:
                    reader = UserProxy.GetFollowingsAsync(this._parent.AccountId);
                    break;
                case RelationDataType.Follower:
                    reader = UserProxy.GetFollowersAsync(this._parent.AccountId);
                    break;
                case RelationDataType.Blocking:
                    reader = UserProxy.GetBlockingsAsync(this._parent.AccountId);
                    break;
                case RelationDataType.NoRetweets:
                    reader = UserProxy.GetNoRetweetsAsync(this._parent.AccountId);
                    break;
                case RelationDataType.Mutes:
                    reader = UserProxy.GetMutesAsync(this._parent.AccountId);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            await this.AddAsync(await reader);
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
                return this._collection.Contains(id);
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
                var result = value ? this._collection.AddDistinct(id) : this._collection.Remove(id);
                if (!result)
                {
                    // not changed
                    return;
                }
            }
            await UserProxy.SetAsync(_type, this._parent.AccountId, id, value);
            this.RaiseAccountDataUpdated(new[] { id }, value);
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
            await RemoveAsync(olds);
            await AddAsync(news);
        }

        private async Task AddAsync(IEnumerable<long> items)
        {
            var m = items.ToArray();
            lock (_collectionLock)
            {
                m = m.Where(i => _collection.AddDistinct(i)).ToArray();
            }
            if (m.Length == 0) return;
            await UserProxy.AddAsync(_type, _parent.AccountId, m);
            this.RaiseAccountDataUpdated(m, true);
        }

        private async Task RemoveAsync(IEnumerable<long> items)
        {
            var m = items.ToArray();
            lock (_collectionLock)
            {
                m = m.Where(i => _collection.Remove(i)).ToArray();
            }
            if (m.Length == 0) return;
            await UserProxy.RemoveAsync(_type, _parent.AccountId, m);
            this.RaiseAccountDataUpdated(m, false);
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
            return this.Type + " " + this.AccountUserId + " => " +
                   this.TargetUserIds.Select(s => s.ToString()).JoinString(", ");
        }
    }

}