using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reactive.Linq;
using StarryEyes.Breezy.DataModel;
using StarryEyes.Models.Stores.Internal;
using StarryEyes.Vanille.DataStore;
using StarryEyes.Vanille.DataStore.Persistent;

namespace StarryEyes.Models.Stores
{
    public static class UserStore
    {
        private const int ChunkCount = 8;

        private static DataStoreBase<long, TwitterUser> _store;

        private static SingleThreadDispatcher<TwitterUser> _dispatcher;

        private static readonly object _snResolverLocker = new object();
        private static readonly SortedDictionary<string, long> _screenNameResolver = new SortedDictionary<string, long>();

        private static volatile bool _isInShutdown;

        public static void Initialize()
        {
            // initialize store
            if (StoreOnMemoryObjectPersistence.IsPersistentDataExisted("users"))
            {
                _store = new PersistentDataStore<long, TwitterUser>
                    (_ => _.Id, Path.Combine(App.DataStorePath, "users"), ChunkCount,
                    manageData: StoreOnMemoryObjectPersistence.GetPersistentData("users"));
            }
            else
            {
                _store = new PersistentDataStore<long, TwitterUser>
                    (_ => _.Id, Path.Combine(App.DataStorePath, "users"), ChunkCount);
            }
            _dispatcher = new SingleThreadDispatcher<TwitterUser>(_store.Store);
            LoadScreenNameResolverCache();
            App.OnApplicationFinalize += Shutdown;
        }

        public static void Store(TwitterUser user)
        {
            if (_isInShutdown) return;
            _dispatcher.Send(user);
            lock (_snResolverLocker)
            {
                _screenNameResolver[user.ScreenName.ToLower()] = user.Id;
            }
        }

        public static IObservable<TwitterUser> Get(long id)
        {
            if (_isInShutdown) return Observable.Empty<TwitterUser>();
            return _store.Get(id);
        }

        public static IObservable<TwitterUser> Get(string screenName)
        {
            if (_isInShutdown) return Observable.Empty<TwitterUser>();
            long id;
            lock (_snResolverLocker)
            {
                if (!_screenNameResolver.TryGetValue(screenName.ToLower(), out id))
                    return Observable.Empty<TwitterUser>();
            }
            return Get(id);
        }

        public static IDictionary<string, long> GetScreenNameResolverTable()
        {
            lock (_snResolverLocker)
            {
                return new Dictionary<string, long>(_screenNameResolver);
            }
        }

        public static long GetId(string screenName)
        {
            lock (_snResolverLocker)
            {
                long id;
                return !_screenNameResolver.TryGetValue(screenName.ToLower(), out id) ? 0 : id;
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
            if (_store != null)
            {
                _store.Dispose();
                var pds = (PersistentDataStore<long, TwitterUser>)_store;
                StoreOnMemoryObjectPersistence.MakePersistent("users", pds.GetManageDatas());
                SaveScreenNameResolverCache();
            }
        }

        private static readonly string _screenNameResolverCacheFile =
            Path.Combine(App.DataStorePath, "snrcache.dat");

        private static void SaveScreenNameResolverCache()
        {
            using (var fs = new FileStream(_screenNameResolverCacheFile,
                FileMode.Create, FileAccess.ReadWrite))
            using (var cs = new DeflateStream(fs, CompressionLevel.Optimal))
            using (var bw = new BinaryWriter(cs))
            {
                bw.Write(_screenNameResolver.Count);
                foreach (var k in _screenNameResolver)
                {
                    bw.Write(k.Key);
                    bw.Write(k.Value);
                }
            }
        }

        private static void LoadScreenNameResolverCache()
        {
            if (File.Exists(_screenNameResolverCacheFile))
            {
                using (var fs = new FileStream(_screenNameResolverCacheFile,
                    FileMode.Open, FileAccess.Read))
                using (var cs = new DeflateStream(fs, CompressionMode.Decompress))
                using (var br = new BinaryReader(cs))
                {
                    int count = br.ReadInt32();
                    for (int i = 0; i < count; i++)
                    {
                        var key = br.ReadString();
                        var value = br.ReadInt64();
                        _screenNameResolver.Add(key.ToLower(), value);
                    }
                }
            }
        }
    }
}