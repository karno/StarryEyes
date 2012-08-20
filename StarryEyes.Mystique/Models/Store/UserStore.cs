using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;
using StarryEyes.SweetLady.DataModel;
using StarryEyes.Vanille.DataStore;
using StarryEyes.Vanille.DataStore.Persistent;

namespace StarryEyes.Mystique.Models.Store
{
    public static class UserStore
    {
        private static DataStoreBase<long, TwitterUser> store;

        private static object snResolverLocker = new object();
        private static SortedDictionary<string, long> screenNameResolver = new SortedDictionary<string, long>();

        static UserStore()
        {
            // initialize store
            store = new PersistentDataStore<long, TwitterUser>
                (_ => _.Id, Path.Combine(App.DataStorePath, "users"), 16);
            App.OnApplicationFinalize += Shutdown;
        }

        public static void Store(TwitterUser user)
        {
            store.Store(user);
            lock (snResolverLocker)
            {
                screenNameResolver[user.ScreenName] = user.Id;
            }
        }

        public static IObservable<TwitterUser> Get(long id)
        {
            return store.Get(id)
                .Do(_ => Store(_));
        }

        public static IObservable<TwitterUser> Get(string screenName)
        {
            long id = 0;
            lock (snResolverLocker)
            {
                if (!screenNameResolver.TryGetValue(screenName, out id))
                    return Observable.Empty<TwitterUser>();
            }
            return Get(id);
        }

        public static IDictionary<string, long> GetScreenNameResolverTable()
        {
            lock (snResolverLocker)
            {
                return new Dictionary<string, long>(screenNameResolver);
            }
        }

        public static IObservable<TwitterUser> Find(Func<TwitterUser, bool> predicate)
        {
            return store.Find(predicate);
        }

        public static void Remove(long id)
        {
            store.Remove(id);
        }

        internal static void Shutdown()
        {
            store.Dispose();
        }
    }
}
