using System;

namespace StarryEyes.Models
{
    public static class SearchFlipModel
    {
        public static event Action<string, SearchMode> OnSearchRequested;

        public static void RequestSearch(string query, SearchMode mode)
        {
            var handler = OnSearchRequested;
            if (handler != null) handler(query, mode);
        }
    }

    public enum SearchMode
    {
        Quick,
        Local,
        Web,
        UserWeb,
        UserScreenName,
    }
}
