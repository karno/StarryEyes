using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Livet;
using StarryEyes.Models.Hubs;
using StarryEyes.Models.Stores;
using StarryEyes.Breezy.Authorize;
using StarryEyes.Breezy.DataModel;
using StarryEyes.Settings;
using StarryEyes.ViewModels.WindowParts.Timelines;

namespace StarryEyes.Models
{
    public class StatusModel
    {
        private static object _staticCacheLock = new object();
        private static SortedDictionary<long, WeakReference> _staticCache = new SortedDictionary<long, WeakReference>();
        private static ConcurrentDictionary<long, object> _generateLock = new ConcurrentDictionary<long, object>();

        public static void UpdateStatusInfo(TwitterStatus status,
            Action<StatusModel> ifCacheIsAlive, Action<TwitterStatus> ifCacheIsDead)
        {
            var lockerobj = _generateLock.GetOrAdd(status.Id, new object());
            try
            {
                lock (lockerobj)
                {
                    StatusModel _model = null;
                    WeakReference wr = null;
                    lock (_staticCacheLock)
                    {
                        _staticCache.TryGetValue(status.Id, out wr);
                    }
                    if (wr != null)
                        _model = (StatusModel)wr.Target;

                    if (_model != null)
                        ifCacheIsAlive(_model);
                    else
                        ifCacheIsDead(status);
                }
            }
            finally
            {
                _generateLock.TryRemove(status.Id, out lockerobj);
            }
        }

        public static StatusModel GetIfCacheIsAlive(long id)
        {
            StatusModel _model = null;
            WeakReference wr = null;
            lock (_staticCacheLock)
            {
                _staticCache.TryGetValue(id, out wr);
            }
            if (wr != null)
                _model = (StatusModel)wr.Target;
            return _model;
        }

        public static StatusModel Get(TwitterStatus status)
        {
            var lockerobj = _generateLock.GetOrAdd(status.Id, new object());
            try
            {
                lock (lockerobj)
                {
                    StatusModel _model = null;
                    WeakReference wr = null;
                    lock (_staticCacheLock)
                    {
                        _staticCache.TryGetValue(status.Id, out wr);
                    }
                    if (wr != null)
                        _model = (StatusModel)wr.Target;

                    if (_model != null)
                        return _model;

                    // cache is dead/not cached yet
                    _model = new StatusModel(status);
                    wr = new WeakReference(_model);
                    lock (_staticCacheLock)
                    {
                        _staticCache[status.Id] = wr;
                    }
                    return _model;
                }
            }
            finally
            {
                _generateLock.TryRemove(status.Id, out lockerobj);
            }
        }

        public static void CollectGarbages()
        {
            long[] values = null;
            lock (_staticCacheLock)
            {
                values = _staticCache.Keys.ToArray();
            }
            foreach (var ids in values.Buffer(16))
            {
                WeakReference wr = null;
                lock (_staticCacheLock)
                {
                    foreach (var id in ids)
                    {
                        _staticCache.TryGetValue(id, out wr);
                        if (wr != null && wr.Target == null)
                            _staticCache.Remove(id);
                    }
                }
                Thread.Sleep(0);
            }
        }

        public TwitterStatus Status { get; private set; }

        private StatusModel(TwitterStatus status)
        {
            this.Status = status;
            status.FavoritedUsers.Guard().ForEach(AddFavoritedUser);
            status.RetweetedUsers.Guard().ForEach(AddRetweetedUser);
        }

        private object _favoritedsLock = new object();
        private readonly SortedDictionary<long, TwitterUser> _favoritedUsersDic = new SortedDictionary<long, TwitterUser>();
        private readonly ObservableSynchronizedCollection<TwitterUser> _favoritedUsers = new ObservableSynchronizedCollection<TwitterUser>();
        public ObservableSynchronizedCollection<TwitterUser> FavoritedUsers
        {
            get { return _favoritedUsers; }
        }

        public void AddFavoritedUser(long userId)
        {
            StoreHub.GetUser(userId).Subscribe(AddFavoritedUser);
        }

        public void AddFavoritedUser(TwitterUser user)
        {
            bool added = false;
            lock (_favoritedsLock)
            {
                if (!_favoritedUsersDic.ContainsKey(user.Id))
                {
                    _favoritedUsersDic.Add(user.Id, user);
                    this.Status.FavoritedUsers = this.Status.FavoritedUsers.Guard().Append(user.Id).ToArray();
                    added = true;
                }
            }
            if (added)
            {
                _favoritedUsers.Add(user);
                StatusStore.Store(this.Status);
            }
        }

        public void RemoveFavoritedUser(long id)
        {
            TwitterUser remove = null;
            lock (_favoritedsLock)
            {
                if (_favoritedUsersDic.TryGetValue(id, out remove))
                    this.Status.FavoritedUsers = this.Status.FavoritedUsers.Except(new[] { id }).ToArray();

            }
            if (remove != null)
            {
                _favoritedUsers.Remove(remove);
                StatusStore.Store(this.Status);
            }
        }

        private object _retweetedsLock = new object();
        private readonly SortedDictionary<long, TwitterUser> _retweetedUsersDic = new SortedDictionary<long, TwitterUser>();
        private readonly ObservableSynchronizedCollection<TwitterUser> _retweetedUsers = new ObservableSynchronizedCollection<TwitterUser>();
        public ObservableSynchronizedCollection<TwitterUser> RetweetedUsers
        {
            get { return _retweetedUsers; }
        }

        public void AddRetweetedUser(long userId)
        {
            StoreHub.GetUser(userId).Subscribe(AddRetweetedUser);
        }

        public void AddRetweetedUser(TwitterUser user)
        {
            bool added = false;
            lock (_retweetedsLock)
            {
                if (!_retweetedUsersDic.ContainsKey(user.Id))
                {
                    _retweetedUsersDic.Add(user.Id, user);
                    this.Status.RetweetedUsers = this.Status.RetweetedUsers.Guard().Append(user.Id).ToArray();
                    added = true;
                }
            }
            if (added)
            {
                _retweetedUsers.Add(user);
                // update persistent info
                StatusStore.Store(this.Status);
            }
        }

        public void RemoveRetweetedUser(long id)
        {
            TwitterUser remove = null;
            lock (_retweetedsLock)
            {
                if (_retweetedUsersDic.TryGetValue(id, out remove))
                    this.Status.RetweetedUsers = this.Status.RetweetedUsers.Except(new[] { id }).ToArray();
            }
            if (remove != null)
            {
                _retweetedUsers.Remove(remove);
                // update persistent info
                StatusStore.Store(this.Status);
            }
        }

        public bool IsFavorited(params long[] ids)
        {
            lock (_favoritedsLock)
            {
                return ids.All(_favoritedUsersDic.ContainsKey);
            }
        }

        public bool IsRetweeted(params long[] ids)
        {
            lock (_retweetedsLock)
            {
                return ids.All(_retweetedUsersDic.ContainsKey);
            }
        }

        public IEnumerable<AuthenticateInfo> GetSuitableReplyAccount()
        {
            var uid = this.Status.InReplyToUserId.GetValueOrDefault();
            if (this.Status.StatusType == StatusType.DirectMessage)
                uid = this.Status.Recipient.Id;
            var info = AccountsStore.GetAccountSetting(uid);
            if (info != null)
            {
                return new[] { BacktrackFallback(info.AuthenticateInfo) };
            }
            else
            {
                return null;
            }
        }

        public AuthenticateInfo BacktrackFallback(AuthenticateInfo info)
        {
            if (!Setting.IsBacktrackFallback.Value)
                return info;
            var cinfo = info;
            while (true)
            {
                var backtrack = AccountsStore.Accounts.Where(i => i.FallbackNext == cinfo.Id)
                    .FirstOrDefault();
                if (backtrack == null)
                    return cinfo;
                else if (backtrack.UserId == info.Id)
                    return info;
                else
                    cinfo = backtrack.AuthenticateInfo;
            }
        }
    }
}
