using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using StarryEyes.Breezy.DataModel;
using System.Threading;

namespace StarryEyes.Models
{
    public class UserModel
    {
        private static object _staticCacheLock = new object();
        private static SortedDictionary<long, WeakReference> _staticCache = new SortedDictionary<long, WeakReference>();
        private static ConcurrentDictionary<long, object> _generateLock = new ConcurrentDictionary<long, object>();

        public static UserModel GetIfCacheIsAlive(long id)
        {
            UserModel _model = null;
            WeakReference wr = null;
            lock (_staticCacheLock)
            {
                _staticCache.TryGetValue(id, out wr);
            }
            if (wr != null)
                _model = (UserModel)wr.Target;
            return _model;
        }

        public static UserModel Get(TwitterUser user)
        {
            var lockerobj = _generateLock.GetOrAdd(user.Id, new object());
            try
            {
                lock (lockerobj)
                {
                    UserModel _model = null;
                    WeakReference wr = null;
                    lock (_staticCacheLock)
                    {
                        _staticCache.TryGetValue(user.Id, out wr);
                    }
                    if (wr != null)
                        _model = (UserModel)wr.Target;

                    if (_model != null)
                        return _model;

                    // cache is dead/not cached yet
                    _model = new UserModel(user);
                    wr = new WeakReference(_model);
                    lock (_staticCacheLock)
                    {
                        _staticCache[user.Id] = wr;
                    }
                    return _model;
                }
            }
            finally
            {
                _generateLock.TryRemove(user.Id, out lockerobj);
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

        public TwitterUser User { get; private set; }

        private UserModel(TwitterUser user)
        {
            this.User = user;
        }
    }
}
