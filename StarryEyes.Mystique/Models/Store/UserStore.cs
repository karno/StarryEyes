using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        private static bool _isInShutdown = false;

        public static void Initialize()
        {
            // initialize store
            if (StoreOnMemoryObjectPersistence.IsPersistentDataExisted("users"))
            {
                store = new PersistentDataStore<long, TwitterUser>
                    (_ => _.Id, Path.Combine(App.DataStorePath, "users"), 16,
                    tocniops: StoreOnMemoryObjectPersistence.GetPersistentData("users"));
            }
            else
            {
                store = new PersistentDataStore<long, TwitterUser>
                    (_ => _.Id, Path.Combine(App.DataStorePath, "users"), 16);
            }
            LoadScreenNameResolverCache();
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
            StoreOnMemoryObjectPersistence.MakePersistent("users", pds.GetToCNIoPs());
            SaveScreenNameResolverCache();
        }

        private static readonly string ScreenNameResolverCacheFile = 
            Path.Combine(App.DataStorePath, "snrcache.dat");

        private static void SaveScreenNameResolverCache()
        {
            using (var fs = new FileStream(ScreenNameResolverCacheFile,
                FileMode.Create, FileAccess.ReadWrite))
            using (var bw = new BinaryWriter(fs))
            {
                bw.Write(screenNameResolver.Count);
                screenNameResolver.ForEach(k =>
                {
                    bw.Write(k.Key);
                    bw.Write(k.Value);
                });
            }
        }

        private static void LoadScreenNameResolverCache()
        {
            if (File.Exists(ScreenNameResolverCacheFile))
            {
                using (var fs = new FileStream(ScreenNameResolverCacheFile,
                    FileMode.Open, FileAccess.Read))
                using (var br = new BinaryReader(fs))
                {
                    int count = br.ReadInt32();
                    for (int i = 0; i < count; i++)
                    {
                        var key = br.ReadString();
                        var value = br.ReadInt64();
                        screenNameResolver.Add(key, value);
                    }
                }
            }
        }
    }
}
