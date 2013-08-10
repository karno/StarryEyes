using StarryEyes.Anomaly.TwitterApi.DataModels;

namespace StarryEyes.Models.Notifications
{
    /// <summary>
    /// Controls around notification.
    /// </summary>
    public static class NotificationModel
    {
        /// <summary>
        /// Request dispatch notification to user.
        /// </summary>
        /// <param name="status">status</param>
        /// <param name="type">notification type</param>
        public static void NotifyNewArrival(TwitterStatus status)
        {
            // TODO: implementation
        }

        private static void NotifyCore(TwitterStatus status, NotificationType type)
        {
            // TODO: implementation
        }
    }

    public enum NotificationType
    {
        Normal,
        Mention,
        DirectMessage,
    }
}
