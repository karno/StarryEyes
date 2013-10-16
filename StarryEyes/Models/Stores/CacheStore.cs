using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reactive.Linq;
using StarryEyes.Albireo;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Models.Receivers;
using StarryEyes.Models.Receiving.Handling;

namespace StarryEyes.Models.Stores
{
    public static class CacheStore
    {
        public static void Initialize()
        {
            StatusBroadcaster.BroadcastPoint
                             .Where(n => n.IsAdded)
                             .Select(n => n.Status)
                             .Subscribe(RegisterStatus);
            try
            {
                lock (_hashtagCache)
                {
                    LoadHashtagCache().ForEach(s => _hashtagCache.AddLast(s));
                }
            }
            catch
            {
                _hashtagCache.Clear();
            }
            try
            {
                lock (_listUserCache)
                {
                    LoadListCache().ForEach(l => SetListUsers(l.Item1, l.Item2));
                }
            }
            catch
            {
            }
            App.ApplicationFinalize += Shutdown;
        }

        private static void Shutdown()
        {
            lock (_hashtagCache)
            {
                SaveHashtagCache(_hashtagCache);
            }
            lock (_listUserCache)
            {
                SaveListCache(_listUserCache);
            }
        }

        #region Hashtag cache

        private static IEnumerable<string> LoadHashtagCache()
        {
            if (!File.Exists(App.HashtagTempFilePath)) yield break;
            using (var fs = File.OpenRead(App.HashtagTempFilePath))
            using (var ds = new DeflateStream(fs, CompressionMode.Decompress))
            using (var br = new BinaryReader(ds))
            {
                var count = br.ReadInt32();
                for (var i = 0; i < count; i++)
                {
                    yield return br.ReadString();
                }
            }
        }

        private static void SaveHashtagCache(IEnumerable<string> cache)
        {
            var items = cache.Where(s => !s.IsNullOrEmpty()).ToArray();
            using (var fs = File.OpenWrite(App.HashtagTempFilePath))
            using (var ds = new DeflateStream(fs, CompressionMode.Compress))
            using (var bw = new BinaryWriter(ds))
            {
                bw.Write(items.Length);
                items.ForEach(i => bw.Write(i));
            }
        }

        public static void RegisterStatus(TwitterStatus status)
        {
            status.Entities
                .Where(e => e.EntityType == EntityType.Hashtags)
                .Select(e => e.DisplayText)
                .ForEach(RegisterHashtag);
        }

        private const int HashtagMaxCount = 2048;

        private static readonly LinkedList<string> _hashtagCache = new LinkedList<string>();

        public static IEnumerable<string> HashtagCache
        {
            get
            {
                lock (_hashtagCache)
                {
                    return _hashtagCache.ToArray();
                }
            }
        }

        public static void RegisterHashtag(string tag)
        {
            lock (_hashtagCache)
            {
                if (_hashtagCache.Any(s => s == tag)) return;
                _hashtagCache.AddLast(tag);
                if (_hashtagCache.Count > HashtagMaxCount)
                {
                    _hashtagCache.RemoveFirst();
                }
            }
        }

        #endregion

        #region List user cache

        private static readonly Dictionary<ListInfo, IEnumerable<long>> _listUserCache = new Dictionary<ListInfo, IEnumerable<long>>();

        private static IEnumerable<Tuple<ListInfo, long[]>> LoadListCache()
        {
            if (!File.Exists(App.ListCacheFileName)) yield break;

            using (var fs = File.OpenRead(App.HashtagTempFilePath))
            using (var ds = new DeflateStream(fs, CompressionMode.Decompress))
            using (var br = new BinaryReader(ds))
            {
                var count = br.ReadInt32();
                for (var i = 0; i < count; i++)
                {
                    var user = br.ReadString();
                    var slug = br.ReadString();
                    var idc = br.ReadInt32();
                    var idl = new List<long>();
                    for (var j = 0; j < idc; j++)
                    {
                        idl.Add(br.ReadInt64());
                    }
                    yield return Tuple.Create(new ListInfo { OwnerScreenName = user, Slug = slug }, idl.ToArray());
                }
            }
        }

        private static void SaveListCache(Dictionary<ListInfo, IEnumerable<long>> cache)
        {
            var items = cache.ToArray();
            using (var fs = File.OpenWrite(App.ListUserTempFilePath))
            using (var ds = new DeflateStream(fs, CompressionMode.Compress))
            using (var bw = new BinaryWriter(ds))
            {
                bw.Write(items.Length);
                items.ForEach(i =>
                {
                    bw.Write(i.Key.OwnerScreenName);
                    bw.Write(i.Key.Slug);
                    var ids = i.Value.ToArray();
                    bw.Write(ids.Length);
                    ids.ForEach(bw.Write);
                });
            }
        }

        public static IEnumerable<long> GetListUsers(ListInfo info)
        {
            lock (_listUserCache)
            {
                IEnumerable<long> users;
                return _listUserCache.TryGetValue(info, out users) ? users : Enumerable.Empty<long>();
            }
        }

        public static void SetListUsers(ListInfo info, IEnumerable<long> users)
        {
            lock (_listUserCache)
            {
                _listUserCache[info] = (users ?? Enumerable.Empty<long>()).ToArray();
            }
        }

        #endregion
    }
}
