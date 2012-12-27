using System;

namespace StarryEyes.Breezy.DataModel
{
    public class TwitterStreamingElement
    {
        public TwitterStreamingElement()
        {
            EventType = DataModel.EventType.Empty;
        }

        public EventType EventType { get; set; }

        public TwitterStatus Status { get; set; }

        public long[] Enumeration { get; set; }

        public int? TrackLimit { get; set; }

        public long? DeletedId { get; set; }

        public TwitterUser EventSourceUser { get; set; }

        public TwitterUser EventTargetUser { get; set; }

        public TwitterStatus EventTargetTweet { get; set; }

        public TwitterList EventTargetList { get; set; }

        public DateTime EventCreatedAt { get; set; }
    }

    public static class TwitterStreamingElementHelper
    {
        public static EventType ToEventType(this string eventString)
        {
            if (String.IsNullOrEmpty(eventString))
                return EventType.Empty;
            switch (eventString)
            {
                case "follow":
                    return EventType.Follow;
                case "unfollow":
                case "remove":
                    return EventType.Unfollow;
                case "favorite":
                    return EventType.Favorite;
                case "unfavorite":
                    return EventType.Unfavorite;
                case "list_user_subscribed":
                    return EventType.ListSubscribed;
                case "list_user_unsubscribed":
                    return EventType.ListUnsubscribed;
                case "list_created":
                    return EventType.ListCreated;
                case "list_destroyed":
                    return EventType.ListDeleted;
                case "list_updated":
                    return EventType.ListUpdated;
                case "list_member_added":
                    return EventType.ListMemberAdded;
                case "list_member_removed":
                    return EventType.ListMemberRemoved;
                case "block":
                    return EventType.Blocked;
                case "unblock":
                    return EventType.Unblocked;
                default:
                    return EventType.Undefined;
            }
        }
    }

    public enum EventType
    {
        /// <summary>
        /// Empty data
        /// </summary>
        Empty,
        /// <summary>
        /// Following
        /// </summary>
        Follow,
        /// <summary>
        /// Unfollow (remove) [CURRENTLY DISABLED]
        /// </summary>
        Unfollow,
        /// <summary>
        /// Favorite
        /// </summary>
        Favorite,
        /// <summary>
        /// Unfavorite
        /// </summary>
        Unfavorite,
        /// <summary>
        /// List created
        /// </summary>
        ListCreated,
        /// <summary>
        /// List deleted
        /// </summary>
        ListDeleted,
        /// <summary>
        /// List information updated
        /// </summary>
        ListUpdated,
        /// <summary>
        /// Added list subscription
        /// </summary>
        ListSubscribed,
        /// <summary>
        /// Deleted list subscription
        /// </summary>
        ListUnsubscribed,
        /// <summary>
        /// List member added
        /// </summary>
        ListMemberAdded,
        /// <summary>
        /// List member removed
        /// </summary>
        ListMemberRemoved,
        /// <summary>
        /// User blocked
        /// </summary>
        Blocked,
        /// <summary>
        /// User unblocked
        /// </summary>
        Unblocked,
        /// <summary>
        /// Track limit info
        /// </summary>
        LimitationInfo,
        /// <summary>
        /// Undefined
        /// </summary>
        Undefined
    }

    public interface ITwitterStreamingElementSpawnable
    {
        TwitterStreamingElement Spawn();
    }
}