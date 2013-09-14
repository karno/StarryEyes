using System;
using StarryEyes.Annotations;
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
        public static void NotifyNewArrival([NotNull] TwitterStatus status)
        {
            if (status == null) throw new ArgumentNullException("status");
            // TODO: implementation
        }

        private static void NotifyCore([NotNull] TwitterStatus status, NotificationType type)
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
