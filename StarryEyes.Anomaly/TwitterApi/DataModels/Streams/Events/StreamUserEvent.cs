using System;

namespace StarryEyes.Anomaly.TwitterApi.DataModels.Streams.Events
{
    /// <summary>
    /// User events
    /// </summary>
    /// <remarks>
    /// This message indicates: events about twitter users
    /// 
    /// This element is supported by: user streams, site streams
    /// (Except: user_update is also used in the (generic) streams)
    /// </remarks>
    public sealed class StreamUserEvent : StreamEvent<TwitterUser, UserEvents>
    {
        public StreamUserEvent(TwitterUser source, TwitterUser target,
            string rawEvent, DateTime createdAt)
            : base(source, target, target, ToEnumEvent(rawEvent), rawEvent, createdAt)
        { }

        public static UserEvents ToEnumEvent(string eventStr)
        {
            switch (eventStr.ToLower())
            {
                case "block":
                    return UserEvents.Block;
                case "unblock":
                    return UserEvents.Unblock;
                case "follow":
                    return UserEvents.Follow;
                case "unfollow":
                    return UserEvents.Unfollow;
                case "user_update":
                    return UserEvents.UserUpdate;
                case "mute":
                    return UserEvents.Mute;
                case "unmute":
                    return UserEvents.UnMute;
                default:
                    return UserEvents.Unknown;
            }
        }
    }


    public enum UserEvents
    {
        Unknown = -1,
        Follow,
        Unfollow,
        Block,
        Unblock,
        UserUpdate,
        Mute,
        UnMute
    }
}
