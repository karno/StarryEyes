using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Livet;
using StarryEyes.Albireo.Threading;
using StarryEyes.Anomaly.Imaging;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Models.Accounting;
using StarryEyes.Models.Databases;
using StarryEyes.Models.Receiving.Handling;
using StarryEyes.Models.Stores;
using StarryEyes.Settings;

namespace StarryEyes.Models.Timelines.Statuses
{
    public class StatusModel
    {
        #region Static members

        private static readonly ConcurrentDictionary<long, WeakReference<StatusModel>> _staticCache =
            new ConcurrentDictionary<long, WeakReference<StatusModel>>();

        private static readonly ConcurrentDictionary<long, object> _generateLock =
            new ConcurrentDictionary<long, object>();

        public static int CachedObjectsCount
        {
            get { return _staticCache.Count; }
        }

        public static StatusModel GetIfCacheIsAlive(long id)
        {
            StatusModel model;
            WeakReference<StatusModel> wr;
            _staticCache.TryGetValue(id, out wr);
            if (wr != null && wr.TryGetTarget(out model))
            {
                return model;
            }
            return null;
        }

        private const int CleanupInterval = 15000;

        private static int _cleanupCount;

        private static readonly TaskFactory _loader = LimitedTaskScheduler.GetTaskFactory(1);

        public static async Task<StatusModel> Get(TwitterStatus status)
        {
            return GetIfCacheIsAlive(status.Id) ?? await _loader.StartNew(async () =>
            {
                if (status.GenerateFromJson)
                {
                    status = await StatusProxy.SyncStatusActivityAsync(status);
                }
                var rto = status.RetweetedOriginal == null
                              ? null
                              : await Get(status.RetweetedOriginal);
                var lockerobj = _generateLock.GetOrAdd(status.Id, new object());
                try
                {
                    lock (lockerobj)
                    {
                        StatusModel model;
                        WeakReference<StatusModel> wr;
                        _staticCache.TryGetValue(status.Id, out wr);
                        if (wr != null && wr.TryGetTarget(out model))
                        {
                            return model;
                        }

                        // cache is dead/not cached yet
                        model = new StatusModel(status, rto);
                        wr = new WeakReference<StatusModel>(model);
                        _staticCache[status.Id] = wr;
                        return model;
                    }
                }
                finally
                {
                    _generateLock.TryRemove(status.Id, out lockerobj);
                    // ReSharper disable InvertIf
#pragma warning disable 4014
                    if (Interlocked.Increment(ref _cleanupCount) == CleanupInterval)
                    {
                        Interlocked.Exchange(ref _cleanupCount, 0);
                        Task.Run((Action)CollectGarbages);
                    }
#pragma warning restore 4014
                    // ReSharper restore InvertIf
                }
            }).Unwrap();
        }

        public static void CollectGarbages()
        {
            var values = _staticCache.Keys.ToArray();
            foreach (var ids in values.Buffer(256))
            {
                foreach (var id in ids)
                {
                    WeakReference<StatusModel> wr;
                    StatusModel target;
                    if (_staticCache.TryGetValue(id, out wr) && !wr.TryGetTarget(out target))
                    {
                        _staticCache.TryRemove(id, out wr);
                    }
                }
                Thread.Sleep(0);
            }
        }

        #endregion

        private readonly TaskFactory _factory = LimitedTaskScheduler.GetTaskFactory(8);

        private readonly ObservableSynchronizedCollectionEx<TwitterUser> _favoritedUsers =
            new ObservableSynchronizedCollectionEx<TwitterUser>();

        private readonly IDictionary<long, TwitterUser> _favoritedUsersDic =
            new SortedDictionary<long, TwitterUser>();

        private readonly object _favoritedsLock = new object();

        private readonly ObservableSynchronizedCollectionEx<TwitterUser> _retweetedUsers =
            new ObservableSynchronizedCollectionEx<TwitterUser>();

        private readonly IDictionary<long, TwitterUser> _retweetedUsersDic =
            new SortedDictionary<long, TwitterUser>();

        private readonly object _retweetedsLock = new object();

        private readonly ObservableSynchronizedCollectionEx<ThumbnailImage> _thumbnails =
            new ObservableSynchronizedCollectionEx<ThumbnailImage>();

        private volatile bool _isFavoritedUsersLoaded;
        private volatile bool _isRetweetedUsersLoaded;

        private StatusModel(TwitterStatus status)
        {
            this.Status = status;
            Task.Run(() =>
            {
                foreach (var image in ImageResolver.ResolveImages(status))
                {
                    _thumbnails.Add(new ThumbnailImage(image));
                }
            });
        }

        private StatusModel(TwitterStatus status, StatusModel retweetedOriginal)
            : this(status)
        {
            this.RetweetedOriginal = retweetedOriginal;
        }

        public TwitterStatus Status { get; private set; }

        public StatusModel RetweetedOriginal { get; private set; }

        public ObservableSynchronizedCollectionEx<TwitterUser> FavoritedUsers
        {
            get
            {
                if (!this._isFavoritedUsersLoaded)
                {
                    this._isFavoritedUsersLoaded = true;
                    _factory.StartNew(this.LoadFavoritedUsers);
                }
                return this._favoritedUsers;
            }
        }

        public ObservableSynchronizedCollectionEx<TwitterUser> RetweetedUsers
        {
            get
            {
                if (!this._isRetweetedUsersLoaded)
                {
                    this._isRetweetedUsersLoaded = true;
                    _factory.StartNew(this.LoadRetweetedUsers);
                }
                return this._retweetedUsers;
            }
        }

        /// <summary>
        /// Image tuples. (original URI, display URI)
        /// </summary>
        public ObservableSynchronizedCollectionEx<ThumbnailImage> Images
        {
            get { return _thumbnails; }
        }

        private void LoadFavoritedUsers()
        {
            if (this.Status.FavoritedUsers != null && this.Status.FavoritedUsers.Length > 0)
            {
                LoadUsers(this.Status.FavoritedUsers, _favoritedsLock, _favoritedUsersDic, _favoritedUsers);
            }
        }

        private void LoadRetweetedUsers()
        {
            if (this.Status.RetweetedUsers != null && this.Status.RetweetedUsers.Length > 0)
            {
                LoadUsers(this.Status.RetweetedUsers, _retweetedsLock, _retweetedUsersDic, _retweetedUsers);
            }
        }

        private static void LoadUsers(IEnumerable<long> users,
            object lockObject,
            IDictionary<long, TwitterUser> dictionary,
            IList<TwitterUser> target)
        {
            var source = users.Reverse().ToArray();
            Task.Run(async () =>
            {
                var loadSource = new HashSet<long>();
                lock (lockObject)
                {
                    foreach (var userId in source)
                    {
                        // check dictionary not contains the id
                        if (dictionary.ContainsKey(userId)) continue;
                        // acquire position
                        dictionary.Add(userId, null);
                        loadSource.Add(userId);
                    }
                }
                var ud = (await StoreHelper.GetUsersAsync(loadSource)).ToDictionary(u => u.Id);
                lock (lockObject)
                {
                    foreach (var userId in source)
                    {
                        TwitterUser user;
                        if (!dictionary.TryGetValue(userId, out user) || user != null)
                        {
                            // user is not in dictionary or
                            // user is already loaded => skip adding
                            continue;
                        }
                        var nu = ud[userId];
                        dictionary[userId] = nu;
                        target.Insert(0, nu);
                    }
                }
            });
        }

        public static void UpdateStatusInfo(long id,
            Action<StatusModel> ifCacheIsAlive, Action<long> ifCacheIsDead)
        {
            WeakReference<StatusModel> wr;
            StatusModel target;
            if (_staticCache.TryGetValue(id, out wr) && wr.TryGetTarget(out target))
            {
                ifCacheIsAlive(target);
            }
            else
            {
                ifCacheIsDead(id);
            }
        }

        public async void AddFavoritedUser(TwitterUser user)
        {
            if (this.Status.RetweetedOriginal != null)
            {
                var status = await Get(this.Status.RetweetedOriginal);
                status.AddFavoritedUser(user);
            }
            else
            {
                var added = false;
                lock (this._favoritedsLock)
                {
                    if (!this._favoritedUsersDic.ContainsKey(user.Id))
                    {
                        this._favoritedUsersDic.Add(user.Id, user);
                        this.Status.FavoritedUsers = this.Status.FavoritedUsers
                                                      .Guard()
                                                      .Append(user.Id)
                                                      .Distinct()
                                                      .ToArray();
                        added = true;
                    }
                }
                if (added)
                {
                    this._favoritedUsers.Add(user);
#pragma warning disable 4014
                    StatusProxy.AddFavoritor(this.Status.Id, user.Id);
                    StatusBroadcaster.Republish(this);
#pragma warning restore 4014
                }
            }
        }

        public async void RemoveFavoritedUser(long userId)
        {
            if (this.Status.RetweetedOriginal != null)
            {
                var status = await Get(this.Status.RetweetedOriginal);
                status.RemoveFavoritedUser(userId);
            }
            else
            {
                TwitterUser remove;
                lock (this._favoritedsLock)
                {
                    if (this._favoritedUsersDic.TryGetValue(userId, out remove))
                    {
                        this._favoritedUsersDic.Remove(userId);
                        this.Status.FavoritedUsers = this.Status.FavoritedUsers.Guard().Except(new[] { userId }).ToArray();
                    }
                }
                if (remove != null)
                {
                    this._favoritedUsers.Remove(remove);
#pragma warning disable 4014
                    StatusProxy.RemoveFavoritor(this.Status.Id, userId);
                    StatusBroadcaster.Republish(this);
#pragma warning restore 4014
                }
            }
        }

        public async void AddRetweetedUser(TwitterUser user)
        {
            if (this.Status.RetweetedOriginal != null)
            {
                var status = await Get(this.Status.RetweetedOriginal);
                status.AddRetweetedUser(user);
            }
            else
            {
                var added = false;
                lock (this._retweetedsLock)
                {
                    if (!this._retweetedUsersDic.ContainsKey(user.Id))
                    {
                        this._retweetedUsersDic.Add(user.Id, user);
                        this.Status.RetweetedUsers = this.Status.RetweetedUsers.Guard()
                                                      .Append(user.Id)
                                                      .Distinct()
                                                      .ToArray();
                        added = true;
                    }
                }
                if (added)
                {
                    this._retweetedUsers.Add(user);
#pragma warning disable 4014
                    StatusProxy.AddRetweeter(this.Status.Id, user.Id);
                    StatusBroadcaster.Republish(this);
#pragma warning restore 4014
                }
            }
        }

        public async void RemoveRetweetedUser(long userId)
        {
            if (this.Status.RetweetedOriginal != null)
            {
                var status = await Get(this.Status.RetweetedOriginal);
                status.RemoveRetweetedUser(userId);
            }
            else
            {
                TwitterUser remove;
                lock (this._retweetedsLock)
                {
                    if (this._retweetedUsersDic.TryGetValue(userId, out remove))
                    {
                        this._retweetedUsersDic.Remove(userId);
                        this.Status.RetweetedUsers = this.Status.RetweetedUsers.Guard().Except(new[] { userId }).ToArray();
                    }
                }
                if (remove != null)
                {
                    this._retweetedUsers.Remove(remove);
                    // update persistent info
#pragma warning disable 4014
                    StatusProxy.RemoveRetweeter(this.Status.Id, userId);
                    StatusBroadcaster.Republish(this);
#pragma warning restore 4014
                }
            }
        }

        public bool IsFavorited(params long[] ids)
        {
            if (ids.Length == 0) return false;
            if (this.Status.RetweetedOriginal != null)
            {
                throw new NotSupportedException("You must create another model indicating RetweetedOriginal status.");
            }
            lock (this._favoritedsLock)
            {
                return ids.All(this._favoritedUsersDic.ContainsKey);
            }
        }

        public bool IsRetweeted(params long[] ids)
        {
            if (ids.Length == 0) return false;
            if (this.Status.RetweetedOriginal != null)
            {
                throw new NotSupportedException("You must create another model indicating RetweetedOriginal status.");
            }
            lock (this._retweetedsLock)
            {
                return ids.All(this._retweetedUsersDic.ContainsKey);
            }
        }

        [CanBeNull]
        public IEnumerable<TwitterAccount> GetSuitableReplyAccount()
        {
            var uid = this.Status.InReplyToUserId.GetValueOrDefault();
            if (this.Status.StatusType == StatusType.DirectMessage)
            {
                if (this.Status.Recipient == null)
                {
                    throw new ArgumentException("Inconsistent status state: Recipient is not spcified in spite of status is direct message.");
                }
                uid = this.Status.Recipient.Id;
            }
            var account = Setting.Accounts.Get(uid);
            return account != null ? new[] { BacktrackFallback(account) } : null;
        }

        public static TwitterAccount BacktrackFallback(TwitterAccount account)
        {
            if (!Setting.IsBacktrackFallback.Value)
            {
                return account;
            }
            var cinfo = account;
            while (true)
            {
                var backtrack = Setting.Accounts.Collection.FirstOrDefault(a => a.FallbackAccountId == cinfo.Id);
                if (backtrack == null)
                    return cinfo;
                if (backtrack.Id == account.Id)
                    return account;
                cinfo = backtrack;
            }
        }

        public override bool Equals(object obj)
        {
            StatusModel another;
            if (obj == null || (another = obj as StatusModel) == null)
            {
                return false;
            }

            return this.Status.Id == another.Status.Id;
        }

        public override int GetHashCode()
        {
            return this.Status.GetHashCode();
        }
    }

    public class ThumbnailImage
    {
        private readonly Uri _display;
        private readonly Uri _source;

        public ThumbnailImage(Uri source, Uri display)
        {
            this._display = display;
            this._source = source;
        }

        public ThumbnailImage(Tuple<Uri, Uri> sourceAndDisplay)
            : this(sourceAndDisplay.Item1, sourceAndDisplay.Item2)
        {
        }

        public Uri SourceUri
        {
            get { return this._source; }
        }

        public Uri DisplayUri
        {
            get { return this._display; }
        }
    }
}
