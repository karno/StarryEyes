using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reactive.Linq;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Models.Receiving.Handling;

namespace StarryEyes.Models.Stores
{
    public static class CacheStore
    {
        public static void Initialize()
        {
            StatusBroadcaster.BroadcastPoint
                             .Where(n => n.IsAdded)
                             .Select(n => n.StatusModel.Status)
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
            App.ApplicationFinalize += Shutdown;
        }

        private static void Shutdown()
        {
            lock (_hashtagCache)
            {
                SaveHashtagCache(_hashtagCache);
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
            var items = cache.Where(s => !String.IsNullOrEmpty(s)).ToArray();
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
    }
}
