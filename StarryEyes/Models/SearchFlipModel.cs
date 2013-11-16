using System;
using StarryEyes.Albireo;

namespace StarryEyes.Models
{
    public static class SearchFlipModel
    {
        public static event Action<string, SearchMode> SearchRequested;

        public static void RequestSearch(string query, SearchMode mode)
        {
            SearchRequested.SafeInvoke(query, mode);
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
