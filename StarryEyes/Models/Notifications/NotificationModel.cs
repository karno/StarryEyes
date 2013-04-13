using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using StarryEyes.Albireo.Data;
using StarryEyes.Breezy.DataModel;

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
