using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;
using StarryEyes.SweetLady.DataModel;
using StarryEyes.Vanille.DataStore;
using StarryEyes.Vanille.DataStore.Persistent;
using System.Xaml;
using System.Threading.Tasks;
using System.Threading;

namespace StarryEyes.Mystique.Models.Store
{
    public static class UserStore
    {
        private static DataStoreBase<long, TwitterUser> store;

        private static object snResolverLocker = new object();
        private static SortedDictionary<string, long> screenNameResolver = new SortedDictionary<string, long>();

        private static bool _isInShutdown = false;

        static UserStore()
        {
            // initialize store
            store = new PersistentDataStore<long, TwitterUser>
                (_ => _.Id, _ => _, Path.Combine(App.DataStorePath, "users"), 16);
            App.OnApplicationFinalize += Shutdown;
        }

        public static void Store(TwitterUser user)
        {
            if (_isInShutdown) return;
            store.Store(user);
            lock (snResolverLocker)
            {
                screenNameResolver[user.ScreenName] = user.Id;
            }
        }

        public static IObservable<TwitterUser> Get(long id)
        {
            if (_isInShutdown) return Observable.Empty<TwitterUser>();
            return store.Get(id)
                .Do(_ => Store(_));
        }

        public static IObservable<TwitterUser> Get(string screenName)
        {
            if (_isInShutdown) return Observable.Empty<TwitterUser>();
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
            if (_isInShutdown) return Observable.Empty<TwitterUser>();
            return store.Find(predicate);
        }

        public static void Remove(long id)
        {
            if (_isInShutdown) return;
            store.Remove(id);
        }

        internal static void Shutdown()
        {
            _isInShutdown = true;
            store.Dispose();
            var pds = (PersistentDataStore<long, TwitterUser>)store;
            StoreOnMemoryObjectPersistence<long>.MakePersistent("user", pds.GetToCNIoPs());
            using (var snf = new FileStream(
                Path.Combine(App.DataStorePath, "snresolve.cache"),
                FileMode.Create, FileAccess.ReadWrite))
            {
                XamlServices.Save(snf, screenNameResolver);
            }
        }
    }
}
