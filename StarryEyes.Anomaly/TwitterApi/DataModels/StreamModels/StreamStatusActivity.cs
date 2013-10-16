using System;

namespace StarryEyes.Anomaly.TwitterApi.DataModels.StreamModels
{
    public class StreamStatusActivity
    {
        public TwitterUser Target { get; set; }

        public TwitterUser Source { get; set; }

        public StreamStatusActivityEvent Event { get; set; }

        public string EventRawString { get; set; }

        public TwitterStatus Status { get; set; }

        public DateTime CreatedAt { get; set; }

        public static StreamStatusActivityEvent ToEnumEvent(string eventStr)
        {
            switch (eventStr.ToLower())
            {
                case "favorite":
                    return StreamStatusActivityEvent.Favorite;
                case "unfavorite":
                    return StreamStatusActivityEvent.Unfavorite;
                default:
                    return StreamStatusActivityEvent.Unknown;
            }
        }
    }

    public enum StreamStatusActivityEvent
    {
        Unknown,
        Favorite,
        Unfavorite,
    }
}
