using System;
using StarryEyes.Breezy.Authorize;
using StarryEyes.Models.Receivers.Managers;
using StarryEyes.Models.Receivers.ReceiveElements;

namespace StarryEyes.Models.Receivers
{
    public static class ReceiversManager
    {
        private static SearchReceiveManager _searchReceiveManager;
        private static ListReceiveManager _listReceiveManager;
        private static UserReceiveManager _userReceiveManager;
        private static StreamTrackReceiveManager _streamTrackReceiveManager;

        public static event Action<long> UserStreamsConnectionStateChanged;

        private static void OnUserStreamsConnectionStateChanged(long obj)
        {
            var handler = UserStreamsConnectionStateChanged;
            if (handler != null) handler(obj);
        }

        public static void Initialize()
        {
            _searchReceiveManager = new SearchReceiveManager();
            _listReceiveManager = new ListReceiveManager();
            _userReceiveManager = new UserReceiveManager();
            _streamTrackReceiveManager = new StreamTrackReceiveManager(_userReceiveManager);
            _userReceiveManager.ConnectionStateChanged += OnUserStreamsConnectionStateChanged;
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

        public static void RegisterList(AuthenticateInfo authInfo, ListInfo info)
        {
            _listReceiveManager.StartReceive(authInfo, info);
        }

        public static void UnregisterList(ListInfo info)
        {
            _listReceiveManager.StopReceive(info);
        }

        public static void ReconnectUserStreams(long id)
        {
            _userReceiveManager.ReconnectStreams(id);
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
