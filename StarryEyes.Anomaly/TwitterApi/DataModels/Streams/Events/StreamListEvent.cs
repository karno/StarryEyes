using System;

namespace StarryEyes.Anomaly.TwitterApi.DataModels.Streams.Events
{
    /// <summary>
    /// List events
    /// </summary>
    /// <remarks>
    /// This message indicates: events about twitter lists
    /// 
    /// This element is supported by: user streams, site streams
    /// </remarks>
    public sealed class StreamListEvent : StreamEvent<TwitterList, ListEvents>
    {
        public StreamListEvent(TwitterUser source, TwitterUser target,
            TwitterList targetObject, string rawEvent, DateTime createdAt)
            : base(source, target, targetObject, ToEnumEvent(rawEvent), rawEvent, createdAt)
        { }

        public static ListEvents ToEnumEvent(string eventStr)
        {
            switch (eventStr)
            {
                case "list_created":
                    return ListEvents.ListCreated;
                case "list_destroyed":
                    return ListEvents.ListDestroyed;
                case "list_updated":
                    return ListEvents.ListUpdated;
                case "list_member_added":
                    return ListEvents.ListMemberAdded;
                case "list_member_removed":
                    return ListEvents.ListMemberRemoved;
                case "list_user_subscribed":
                    return ListEvents.ListUserSubscribed;
                case "list_user_unsubscribed":
                    return ListEvents.ListUserUnsubscribed;
                default:
                    return ListEvents.Unknown;
            }
        }
    }

    public enum ListEvents
    {
        Unknown,
        ListCreated,
        ListDestroyed,
        ListUpdated,
        ListMemberAdded,
        ListMemberRemoved,
        ListUserSubscribed,
        ListUserUnsubscribed,
    }
}
