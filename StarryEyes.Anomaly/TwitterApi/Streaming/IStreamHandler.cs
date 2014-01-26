using System;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.TwitterApi.DataModels.StreamModels;

namespace StarryEyes.Anomaly.TwitterApi.Streaming
{
    public interface IStreamHandler
    {
        void OnStatus(TwitterStatus status);

        void OnDeleted(StreamDelete item);

        void OnDisconnect(StreamDisconnect streamDisconnect);

        void OnEnumerationReceived(StreamEnumeration item);

        void OnListActivity(StreamListActivity item);

        void OnStatusActivity(StreamStatusActivity item);

        void OnTrackLimit(StreamTrackLimit item);

        void OnUserActivity(StreamUserActivity item);

        void OnExceptionThrownDuringParsing(Exception ex);
    }
}
