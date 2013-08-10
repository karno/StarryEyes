using System;

namespace StarryEyes.Anomaly.TwitterApi.DataModels.StreamModels
{
    public class StreamListActivity
    {
        public TwitterUser Target { get; set; }

        public TwitterUser Source { get; set; }

        public StreamListActivityEvent Event { get; set; }

        public string EventRawString { get; set; }

        public TwitterList List { get; set; }

        public DateTime CreatedAt { get; set; }


        public static StreamListActivityEvent ToEnumEvent(string eventStr)
        {
            switch (eventStr)
            {
                case "list_created":
                    return StreamListActivityEvent.ListCreated;
                case "list_destroyed":
                    return StreamListActivityEvent.ListDestroyed;
                case "list_updated":
                    return StreamListActivityEvent.ListUpdated;
                case "list_member_added":
                    return StreamListActivityEvent.ListMemberAdded;
                case "list_member_removed":
                    return StreamListActivityEvent.ListMemberRemoved;
                case "list_user_subscribed":
                    return StreamListActivityEvent.ListUserSubscribed;
                case "list_user_unsubscribed":
                    return StreamListActivityEvent.ListUserUnsubscribed;
                default:
                    return StreamListActivityEvent.Unknown;
            }
        }
    }

    public enum StreamListActivityEvent
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
