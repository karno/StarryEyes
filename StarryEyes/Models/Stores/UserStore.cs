using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reactive.Linq;
using StarryEyes.Breezy.DataModel;
using StarryEyes.Models.Stores.Internal;
using StarryEyes.Vanille.DataStore;
using StarryEyes.Vanille.DataStore.Persistent;

namespace StarryEyes.Models.Stores
{
    public static class UserStore
    {
        private static DataStoreBase<long, TwitterUser> _store;

        private static readonly object SnResolverLocker = new object();
        private static readonly SortedDictionary<string, long> ScreenNameResolver = new SortedDictionary<string, long>();

        private static volatile bool _isInShutdown = false;

        public static void Initialize()
        {
            // initialize store
            if (StoreOnMemoryObjectPersistence.IsPersistentDataExisted("users"))
            {
                _store = new PersistentDataStore<long, TwitterUser>
                    (_ => _.Id, Path.Combine(App.DataStorePath, "users"), chunkNum: 16,
                    tocniops: StoreOnMemoryObjectPersistence.GetPersistentData("users"));
            }
            else
            {
                _store = new PersistentDataStore<long, TwitterUser>
                    (_ => _.Id, Path.Combine(App.DataStorePath, "users"), chunkNum: 16);
            }
            LoadScreenNameResolverCache();
            App.OnApplicationFinalize += Shutdown;
        }

        public static void Store(TwitterUser user)
        {
            if (_isInShutdown) return;
            _store.Store(user);
            lock (SnResolverLocker)
            {
                ScreenNameResolver[user.ScreenName] = user.Id;
            }
        }

        public static IObservable<TwitterUser> Get(long id)
        {
            if (_isInShutdown) return Observable.Empty<TwitterUser>();
            return _store.Get(id)
                .Do(_ => Store(_));
        }

        public static IObservable<TwitterUser> Get(string screenName)
        {
            if (_isInShutdown) return Observable.Empty<TwitterUser>();
            long id = 0;
            lock (SnResolverLocker)
            {
                if (!ScreenNameResolver.TryGetValue(screenName, out id))
                    return Observable.Empty<TwitterUser>();
            }
            return Get(id);
        }

        public static IDictionary<string, long> GetScreenNameResolverTable()
        {
            lock (SnResolverLocker)
            {
                return new Dictionary<string, long>(ScreenNameResolver);
            }
        }

        public static IObservable<TwitterUser> Find(Func<TwitterUser, bool> predicate)
        {
            if (_isInShutdown) return Observable.Empty<TwitterUser>();
            return _store.Find(predicate);
        }

        public static void Remove(long id)
        {
            if (_isInShutdown) return;
            _store.Remove(id);
        }

        internal static void Shutdown()
        {
            _isInShutdown = true;
            _store.Dispose();
            var pds = (PersistentDataStore<long, TwitterUser>)_store;
            StoreOnMemoryObjectPersistence.MakePersistent("users", pds.GetToCNIoPs());
            SaveScreenNameResolverCache();
        }

        private static readonly string ScreenNameResolverCacheFile =
            Path.Combine(App.DataStorePath, "snrcache.dat");

        private static void SaveScreenNameResolverCache()
        {
            using (var fs = new FileStream(ScreenNameResolverCacheFile,
                FileMode.Create, FileAccess.ReadWrite))
            using (var cs = new DeflateStream(fs, CompressionLevel.Optimal))
            using (var bw = new BinaryWriter(fs))
            {
                bw.Write(ScreenNameResolver.Count);
                ScreenNameResolver.ForEach(k =>
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
                using (var cs = new DeflateStream(fs, CompressionMode.Decompress))
                using (var br = new BinaryReader(fs))
                {
                    int count = br.ReadInt32();
                    for (int i = 0; i < count; i++)
                    {
                        var key = br.ReadString();
                        var value = br.ReadInt64();
                        ScreenNameResolver.Add(key, value);
                    }
                }
            }
        }
    }
}