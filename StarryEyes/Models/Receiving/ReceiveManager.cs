using System;
using StarryEyes.Albireo;
using StarryEyes.Models.Accounting;
using StarryEyes.Models.Receivers;
using StarryEyes.Models.Receiving.Managers;
using StarryEyes.Models.Receiving.Receivers;

namespace StarryEyes.Models.Receiving
{
    public static class ReceiveManager
    {
        private static SearchReceiveManager _searchReceiveManager;
        private static ListReceiveManager _listReceiveManager;
        private static UserReceiveManager _userReceiveManager;
        private static StreamTrackReceiveManager _streamTrackReceiveManager;

        public static event Action<TwitterAccount> UserStreamsConnectionStateChanged;

        public static void Initialize()
        {
            BehaviorLogger.Log("RM", "initializing...");
            _userReceiveManager = new UserReceiveManager();
            _searchReceiveManager = new SearchReceiveManager();
            _listReceiveManager = new ListReceiveManager();
            _streamTrackReceiveManager = new StreamTrackReceiveManager(_userReceiveManager);
            _userReceiveManager.ConnectionStateChanged += arg => UserStreamsConnectionStateChanged.SafeInvoke(arg);
            BehaviorLogger.Log("RM", "init.");
        }

        public static void RegisterSearchQuery(string query)
        {
            _searchReceiveManager.RegisterSearchQuery(query);
        }

        public static void UnregisterSearchQuery(string query)
        {
            _searchReceiveManager.UnregisterSearchQuery(query);
        }

        public static void RegisterStreamingQuery(string query)
        {
            _streamTrackReceiveManager.AddTrackKeyword(query);
        }

        public static void UnregisterStreamingQuery(string query)
        {
            _streamTrackReceiveManager.RemoveTrackKeyword(query);
        }

        public static void RegisterList(ListInfo info)
        {
            _listReceiveManager.StartReceive(info);
        }

        public static void RegisterList(string receiver, ListInfo info)
        {
            _listReceiveManager.StartReceive(receiver, info);
        }

        public static void RegisterList(TwitterAccount authInfo, ListInfo info)
        {
            _listReceiveManager.StartReceive(authInfo, info);
        }

        public static void UnregisterList(ListInfo info)
        {
            _listReceiveManager.StopReceive(info);
        }

        public static void ReconnectUserStreams(long id)
        {
            _userReceiveManager.ReconnectStream(id);
        }

        public static void ReconnectUserStreams()
        {
            _userReceiveManager.ReconnectAllStreams();
        }

        public static UserStreamsConnectionState GetConnectionState(long id)
        {
            return _userReceiveManager.GetConnectionState(id);
        }
    }
}
