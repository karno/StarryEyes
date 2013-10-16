using System;

namespace StarryEyes.Anomaly.TwitterApi.DataModels.StreamModels
{
    public class StreamUserActivity
    {
        public TwitterUser Target { get; set; }

        public TwitterUser Source { get; set; }

        public StreamUserActivityEvent Event { get; set; }

        public string EventRawString { get; set; }

        public DateTime CreatedAt { get; set; }

        public static StreamUserActivityEvent ToEnumEvent(string eventStr)
        {
            switch (eventStr.ToLower())
            {
                case "block":
                    return StreamUserActivityEvent.Block;
                case "unblock":
                    return StreamUserActivityEvent.Unblock;
                case "follow":
                    return StreamUserActivityEvent.Follow;
                case "unfollow":
                    return StreamUserActivityEvent.Unfollow;
                case "user_update":
                    return StreamUserActivityEvent.UserUpdate;
                default:
                    return StreamUserActivityEvent.Unknown;
            }
        }
    }


    public enum StreamUserActivityEvent
    {
        Unknown,
        Follow,
        Unfollow,
        Block,
        Unblock,
        UserUpdate,
    }
}
