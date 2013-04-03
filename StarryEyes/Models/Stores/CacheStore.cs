using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
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

        public static void RegisterStatus(TwitterStatus status)
        {
            status.Entities
                .Where(e => e.EntityType == EntityType.Hashtags)
                .Select(e => e.DisplayText)
                .ForEach(RegisterHashtag);
        }

        private const int HashtagMaxCount = 1024;

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
