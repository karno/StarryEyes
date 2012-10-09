using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Livet;
using StarryEyes.Models.Hub;
using StarryEyes.Models.Store;
using StarryEyes.Moon.Authorize;
using StarryEyes.Moon.DataModel;
using StarryEyes.Settings;
using StarryEyes.ViewModels.WindowParts.Timeline;

namespace StarryEyes.Models
{
    public class StatusProxy
    {
        private static object _staticCacheLock = new object();
        private static SortedDictionary<long, WeakReference> _staticCache = new SortedDictionary<long, WeakReference>();
        private static ConcurrentDictionary<long, object> _generateLock = new ConcurrentDictionary<long, object>();

        public static void UpdateStatusInfo(TwitterStatus status,
            Action<StatusProxy> ifCacheIsAlive, Action<TwitterStatus> ifCacheIsDead)
        {
            var lockerobj = _generateLock.GetOrAdd(status.Id, new object());
            try
            {
                lock (lockerobj)
                {
                    StatusProxy _proxy = null;
                    WeakReference wr = null;
                    lock (_staticCacheLock)
                    {
                        _staticCache.TryGetValue(status.Id, out wr);
                    }
                    if (wr != null)
                        _proxy = (StatusProxy)wr.Target;

                    if (_proxy != null)
                        ifCacheIsAlive(_proxy);
                    else
                        ifCacheIsDead(status);
                }
            }
            finally
            {
                _generateLock.TryRemove(status.Id, out lockerobj);
            }
        }

        public static StatusProxy GetIfCacheIsAlive(long id)
        {
            StatusProxy _proxy = null;
            WeakReference wr = null;
            lock (_staticCacheLock)
            {
                _staticCache.TryGetValue(id, out wr);
            }
            if (wr != null)
                _proxy = (StatusProxy)wr.Target;
            return _proxy;
        }

        public static StatusProxy Get(TwitterStatus status)
        {
            var lockerobj = _generateLock.GetOrAdd(status.Id, new object());
            try
            {
                lock (lockerobj)
                {
                    StatusProxy _proxy = null;
                    WeakReference wr = null;
                    lock (_staticCacheLock)
                    {
                        _staticCache.TryGetValue(status.Id, out wr);
                    }
                    if (wr != null)
                        _proxy = (StatusProxy)wr.Target;

                    if (_proxy != null)
                        return _proxy;

                    // not alived/not cached yet
                    _proxy = new StatusProxy(status);
                    wr = new WeakReference(_proxy);
                    lock (_staticCacheLock)
                    {
                        _staticCache[status.Id] = wr;
                    }
                    return _proxy;
                }
            }
            finally
            {
                _generateLock.TryRemove(status.Id, out lockerobj);
            }
        }

        public static void SweepGarbages()
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

        private StatusProxy(TwitterStatus status)
        {
            this.Status = status;
            status.FavoritedUsers.Guard().ForEach(AddFavoritedUser);
            status.RetweetedUsers.Guard().ForEach(AddRetweetedUser);

            __fuwrap = new ReadOnlyDispatcherCollection<UserViewModel>(
                new DispatcherCollection<UserViewModel>(
                    _favoritedUsers, DispatcherHelper.UIDispatcher));
            __ruwrap = new ReadOnlyDispatcherCollection<UserViewModel>(
                new DispatcherCollection<UserViewModel>(
                    _retweetedUsers, DispatcherHelper.UIDispatcher));
        }

        private object _favoritedsLock = new object();
        private SortedDictionary<long, UserViewModel> _favoritedUsersDic = new SortedDictionary<long, UserViewModel>();
        private ObservableSynchronizedCollection<UserViewModel> _favoritedUsers = new ObservableSynchronizedCollection<UserViewModel>();

        private ReadOnlyDispatcherCollection<UserViewModel> __fuwrap;
        public ReadOnlyDispatcherCollection<UserViewModel> FavoritedUsers
        {
            get { return __fuwrap; }
        }

        public void AddFavoritedUser(long userId)
        {
            StoreHub.GetUser(userId).Subscribe(AddFavoritedUser);
        }

        public void AddFavoritedUser(TwitterUser user)
        {
            UserViewModel _add = null;
            lock (_favoritedsLock)
            {
                if (!_favoritedUsersDic.ContainsKey(user.Id))
                {
                    _add = new UserViewModel(user);
                    _favoritedUsersDic.Add(user.Id, _add);
                    this.Status.FavoritedUsers = this.Status.FavoritedUsers.Append(user.Id).ToArray();
                }
            }
            if (_add != null)
            {
                _favoritedUsers.Add(_add);
                StatusStore.Store(this.Status);
            }
        }

        public void RemoveFavoritedUser(long id)
        {
            UserViewModel _remove = null;
            lock (_favoritedsLock)
            {
                if (_favoritedUsersDic.TryGetValue(id, out _remove))
                    this.Status.FavoritedUsers = this.Status.FavoritedUsers.Except(new[] { id }).ToArray();

            }
            if (_remove != null)
            {
                _favoritedUsers.Remove(_remove);
                StatusStore.Store(this.Status);
            }
        }

        private object _retweetedsLock = new object();
        private SortedDictionary<long, UserViewModel> _retweetedUsersDic = new SortedDictionary<long, UserViewModel>();
        private ObservableSynchronizedCollection<UserViewModel> _retweetedUsers = new ObservableSynchronizedCollection<UserViewModel>();

        private ReadOnlyDispatcherCollection<UserViewModel> __ruwrap;
        public ReadOnlyDispatcherCollection<UserViewModel> RetweetedUsers
        {
            get { return __ruwrap; }
        }

        public void AddRetweetedUser(long userId)
        {
            StoreHub.GetUser(userId).Subscribe(AddRetweetedUser);
        }

        public void AddRetweetedUser(TwitterUser user)
        {
            UserViewModel _add = null;
            lock (_retweetedsLock)
            {
                if (!_retweetedUsersDic.ContainsKey(user.Id))
                {
                    _add = new UserViewModel(user);
                    _retweetedUsersDic.Add(user.Id, _add);
                    this.Status.RetweetedUsers = this.Status.RetweetedUsers.Append(user.Id).ToArray();
                }
            }
            if (_add != null)
            {
                _retweetedUsers.Add(_add);
                // update persistent info
                StatusStore.Store(this.Status);
            }
        }

        public void RemoveRetweetedUser(long id)
        {
            UserViewModel _remove = null;
            lock (_retweetedsLock)
            {
                if (_retweetedUsersDic.TryGetValue(id, out _remove))
                    this.Status.RetweetedUsers = this.Status.RetweetedUsers.Except(new[] { id }).ToArray();
            }
            if (_remove != null)
            {
                _retweetedUsers.Remove(_remove);
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
            var info = Setting.LookupAccountSetting(uid);
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
                var backtrack = Setting.Accounts.Where(i => i.FallbackNext == cinfo.Id)
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
