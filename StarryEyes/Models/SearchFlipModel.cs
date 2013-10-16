using System;

namespace StarryEyes.Models
{
    public static class SearchFlipModel
    {
        public static event Action<string, SearchMode> SearchRequested;

        public static void RequestSearch(string query, SearchMode mode)
        {
            var handler = SearchRequested;
            if (handler != null) handler(query, mode);
        }
    }

    public enum SearchMode
    {
        CurrentTab,
        Local,
        Web,
        UserWeb,
        UserScreenName,
    }
}
