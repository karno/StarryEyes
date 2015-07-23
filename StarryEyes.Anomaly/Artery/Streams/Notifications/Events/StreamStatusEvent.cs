using System;
using StarryEyes.Anomaly.TwitterApi.DataModels;

namespace StarryEyes.Anomaly.Artery.Streams.Notifications.Events
{
    /// <summary>
    /// Status events
    /// </summary>
    /// <remarks>
    /// This notification indicates: events about twitter statuses 
    /// 
    /// This element is supported by: user streams, site streams
    /// </remarks>
    public sealed class StreamStatusEvent : StreamEvent<TwitterStatus, StatusEvents>
    {
        public StreamStatusEvent(TwitterUser source, TwitterUser target,
            TwitterStatus targetObject, string rawEvent, DateTime createdAt)
            : base(source, target, targetObject, ToEnumEvent(rawEvent), rawEvent, createdAt) { }

        private static StatusEvents ToEnumEvent(string eventStr)
        {
            switch (eventStr.ToLower())
            {
                case "favorite":
                    return StatusEvents.Favorite;
                case "unfavorite":
                    return StatusEvents.Unfavorite;
                case "quoted_tweet":
                    return StatusEvents.Quote;
                case "favorited_retweet":
                    return StatusEvents.FavoriteRetweet;
                case "retweeted_retweet":
                    return StatusEvents.RetweetRetweet;
                default:
                    return StatusEvents.Unknown;
            }
        }
    }

    public enum StatusEvents
    {
        Unknown = -1,
        Favorite,
        Unfavorite,
        FavoriteRetweet,
        RetweetRetweet,
        Quote,
    }
}
