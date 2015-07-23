using System;
using StarryEyes.Anomaly.Artery.Streams.Notifications.Events;
using StarryEyes.Anomaly.Artery.Streams.Notifications;
using StarryEyes.Anomaly.TwitterApi.DataModels;

namespace StarryEyes.Anomaly.Artery.Streams
{
    public interface IOldStreamHandler
    {
        void OnStatus(TwitterStatus status);

        void OnDeleted(StreamDelete item);

        void OnDisconnect(StreamDisconnect streamDisconnect);

        void OnEnumerationReceived(StreamEnumeration item);

        void OnListActivity(StreamListEvent item);

        void OnStatusActivity(StreamStatusEvent item);

        void OnTrackLimit(StreamLimit item);

        void OnUserActivity(StreamUserEvent item);

        void OnExceptionThrownDuringParsing(Exception ex);
    }
}
