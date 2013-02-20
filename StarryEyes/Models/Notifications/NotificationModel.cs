using System;
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
        private static object _acceptLock = new object();
        private static AVLTree<long> _acceptingStatusIds = new AVLTree<long>();

        /// <summary>
        /// Start accept notification about status.
        /// </summary>
        /// <param name="status"></param>
        /// <param name="period"></param>
        public static void StartAccept(TwitterStatus status, TimeSpan period)
        {
            lock (_acceptLock)
            {
                _acceptingStatusIds.Add(status.Id);
            }
            Observable.Timer(period)
                      .Subscribe(_ =>
                      {
                          lock (_acceptLock)
                          {
                              _acceptingStatusIds.Remove(status.Id);
                          }
                      });
        }

        /// <summary>
        /// Request dispatch notification to user.
        /// </summary>
        /// <param name="status"></param>
        public static void NotifyNewArrival(TwitterStatus status, NotificationType type = NotificationType.Normal)
        {
            // TODO: Implement this.
            bool immediate = false;
            switch (type)
            {
                case NotificationType.Normal:
                    break;
                case NotificationType.Mention:
                    immediate = true;
                    break;
                case NotificationType.DirectMessage:
                    immediate = true;
                    break;
            }
        }
    }

    public enum NotificationType
    {
        Normal,
        Mention,
        DirectMessage,
    }
}
