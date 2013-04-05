using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reactive.Linq;
using StarryEyes.Albireo;
using StarryEyes.Breezy.DataModel;

namespace StarryEyes.Models.Stores
{
    public static class CacheStore
    {
        static CacheStore()
        {
            StatusStore.StatusPublisher
                       .Where(s => s.IsAdded)
                       .Select(s => s.Status)
                       .Subscribe(RegisterStatus);
        }

        public static void Initialize()
        {
            try
            {
                lock (_hashtagCache)
                {
                    LoadCache().ForEach(s => _hashtagCache.AddLast(s));
                }
            }
            catch
            {
                _hashtagCache.Clear();
            }
            App.OnApplicationFinalize += Shutdown;
        }

        private static void Shutdown()
        {
            SaveCache(HashtagCache);
        }

        private static IEnumerable<string> LoadCache()
        {
            if (!File.Exists(App.HashtagTempFilePath)) yield break;
            using (var fs = File.OpenRead(App.HashtagTempFilePath))
            using (var ds = new DeflateStream(fs, CompressionMode.Decompress))
            using (var br = new BinaryReader(ds))
            {
                int count = br.ReadInt32();
                for (int i = 0; i < count; i++)
                {
                    yield return br.ReadString();
                }
            }
        }

        private static void SaveCache(IEnumerable<string> cache)
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
    }
}
