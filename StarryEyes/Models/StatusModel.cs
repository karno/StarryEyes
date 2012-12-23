using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using Livet;
using StarryEyes.Breezy.Authorize;
using StarryEyes.Breezy.DataModel;
using StarryEyes.Models.Hubs;
using StarryEyes.Models.Stores;
using StarryEyes.Settings;

namespace StarryEyes.Models
{
    public class StatusModel
    {
        private static readonly object _staticCacheLock = new object();

        private static readonly SortedDictionary<long, WeakReference> _staticCache =
            new SortedDictionary<long, WeakReference>();

        private static readonly ConcurrentDictionary<long, object> _generateLock =
            new ConcurrentDictionary<long, object>();

        private readonly ObservableSynchronizedCollection<TwitterUser> _favoritedUsers =
            new ObservableSynchronizedCollection<TwitterUser>();

        private readonly SortedDictionary<long, TwitterUser> _favoritedUsersDic =
            new SortedDictionary<long, TwitterUser>();

        private readonly object _favoritedsLock = new object();

        private readonly ObservableSynchronizedCollection<TwitterUser> _retweetedUsers =
            new ObservableSynchronizedCollection<TwitterUser>();

        private readonly SortedDictionary<long, TwitterUser> _retweetedUsersDic =
            new SortedDictionary<long, TwitterUser>();

        private readonly object _retweetedsLock = new object();

        private StatusModel(TwitterStatus status)
        {
            Status = status;
            status.FavoritedUsers.Guard()
                  .Distinct()
                  .ToObservable()
                  .Do(_ =>
                  {
                      lock (_favoritedsLock)
                      {
                          _favoritedUsersDic.Add(_, null);
                      }
                  })
                  .SelectMany(StoreHub.GetUser)
                  .Do(_ =>
                  {
                      lock (_favoritedsLock)
                      {
                          _favoritedUsersDic[_.Id] = _;
                      }
                  })
                  .Subscribe(_favoritedUsers.Add);
            status.RetweetedUsers.Guard()
                  .Distinct()
                  .ToObservable()
                  .Do(_ =>
                  {
                      lock (_retweetedsLock)
                      {
                          _retweetedUsersDic.Add(_, null);
                      }
                  })
                  .SelectMany(StoreHub.GetUser)
                  .Do(_ =>
                  {
                      lock (_retweetedsLock)
                      {
                          _retweetedUsersDic[_.Id] = _;
                      }
                  })
                  .Subscribe(_retweetedUsers.Add);
        }


        public TwitterStatus Status { get; private set; }

        public ObservableSynchronizedCollection<TwitterUser> FavoritedUsers
        {
            get { return _favoritedUsers; }
        }

        public ObservableSynchronizedCollection<TwitterUser> RetweetedUsers
        {
            get { return _retweetedUsers; }
        }

        public static void UpdateStatusInfo(TwitterStatus status,
                                            Action<StatusModel> ifCacheIsAlive, Action<TwitterStatus> ifCacheIsDead)
        {
            object lockerobj = _generateLock.GetOrAdd(status.Id, new object());
            try
            {
                lock (lockerobj)
                {
                    StatusModel model = null;
                    WeakReference wr;
                    lock (_staticCacheLock)
                    {
                        _staticCache.TryGetValue(status.Id, out wr);
                    }
                    if (wr != null)
                        model = (StatusModel)wr.Target;

                    if (model != null)
                        ifCacheIsAlive(model);
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
            StatusModel model = null;
            WeakReference wr;
            lock (_staticCacheLock)
            {
                _staticCache.TryGetValue(id, out wr);
            }
            if (wr != null)
                model = (StatusModel)wr.Target;
            return model;
        }

        public static StatusModel Get(TwitterStatus status)
        {
            object lockerobj = _generateLock.GetOrAdd(status.Id, new object());
            try
            {
                lock (lockerobj)
                {
                    StatusModel model = null;
                    WeakReference wr;
                    lock (_staticCacheLock)
                    {
                        _staticCache.TryGetValue(status.Id, out wr);
                    }
                    if (wr != null)
                        model = (StatusModel)wr.Target;

                    if (model != null)
                        return model;

                    // cache is dead/not cached yet
                    model = new StatusModel(status);
                    wr = new WeakReference(model);
                    lock (_staticCacheLock)
                    {
                        _staticCache[status.Id] = wr;
                    }
                    return model;
                }
            }
            finally
            {
                _generateLock.TryRemove(status.Id, out lockerobj);
            }
        }

        public static void CollectGarbages()
        {
            long[] values;
            lock (_staticCacheLock)
            {
                values = _staticCache.Keys.ToArray();
            }
            foreach (var ids in values.Buffer(16))
            {
                lock (_staticCacheLock)
                {
                    foreach (long id in ids)
                    {
                        WeakReference wr;
                        _staticCache.TryGetValue(id, out wr);
                        if (wr != null && wr.Target == null)
                            _staticCache.Remove(id);
                    }
                }
                Thread.Sleep(0);
            }
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
                    Status.FavoritedUsers = Status.FavoritedUsers.Guard().Append(user.Id).Distinct().ToArray();
                    added = true;
                }
            }
            if (added)
            {
                _favoritedUsers.Add(user);
                StatusStore.Store(Status);
            }
        }

        public void RemoveFavoritedUser(long id)
        {
            TwitterUser remove;
            lock (_favoritedsLock)
            {
                if (_favoritedUsersDic.TryGetValue(id, out remove))
                    Status.FavoritedUsers = Status.FavoritedUsers.Except(new[] { id }).ToArray();
            }
            if (remove != null)
            {
                _favoritedUsers.Remove(remove);
                StatusStore.Store(Status);
            }
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
                    Status.RetweetedUsers = Status.RetweetedUsers.Guard().Append(user.Id).Distinct().ToArray();
                    added = true;
                }
            }
            if (added)
            {
                _retweetedUsers.Add(user);
                // update persistent info
                StatusStore.Store(Status);
            }
        }

        public void RemoveRetweetedUser(long id)
        {
            TwitterUser remove;
            lock (_retweetedsLock)
            {
                if (_retweetedUsersDic.TryGetValue(id, out remove))
                    Status.RetweetedUsers = Status.RetweetedUsers.Except(new[] { id }).ToArray();
            }
            if (remove != null)
            {
                _retweetedUsers.Remove(remove);
                // update persistent info
                StatusStore.Store(Status);
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
            long uid = Status.InReplyToUserId.GetValueOrDefault();
            if (Status.StatusType == StatusType.DirectMessage)
                uid = Status.Recipient.Id;
            AccountSetting info = AccountsStore.GetAccountSetting(uid);
            if (info != null)
            {
                return new[] { BacktrackFallback(info.AuthenticateInfo) };
            }
            return null;
        }

        public AuthenticateInfo BacktrackFallback(AuthenticateInfo info)
        {
            if (!Setting.IsBacktrackFallback.Value)
                return info;
            AuthenticateInfo cinfo = info;
            while (true)
            {
                AccountSetting backtrack = AccountsStore.Accounts
                                                        .FirstOrDefault(i => i.FallbackNext == cinfo.Id);
                if (backtrack == null)
                    return cinfo;
                if (backtrack.UserId == info.Id)
                    return info;
                cinfo = backtrack.AuthenticateInfo;
            }
        }
    }
}