using System;
using System.Collections.Generic;
using Cadena.Data;
using StarryEyes.Albireo.Helpers;
using StarryEyes.ViewModels.Notifications;

namespace StarryEyes.Models.Subsystems.Notifications.UI
{
    public class SlimNotificator : IUINotificator
    {
        private static readonly SlimNotificator _instance = new SlimNotificator();

        public static SlimNotificator Instance
        {
            get { return _instance; }
        }

        static SlimNotificator()
        {
            SlimNotificatorViewModel.Initialize();
        }

        private readonly LinkedList<NotificationData> _urgentPriorityQueue = new LinkedList<NotificationData>();

        private readonly LinkedList<NotificationData> _highPriorityQueue = new LinkedList<NotificationData>();

        private readonly LinkedList<NotificationData> _middlePriorityQueue = new LinkedList<NotificationData>();

        private readonly LinkedList<NotificationData> _lowPriorityQueue = new LinkedList<NotificationData>();

        private readonly LinkedList<NotificationData>[] _queues;

        public event Action OnNewNotificationDataQueued;

        public SlimNotificator()
        {
            _queues = new[]
            {
                _urgentPriorityQueue,
                _highPriorityQueue,
                _middlePriorityQueue,
                _lowPriorityQueue
            };
        }

        public NotificationData GetQueuedNotification()
        {
            foreach (var list in _queues)
            {
                lock (list)
                {
                    if (list.Count > 0)
                    {
                        var item = list.First;
                        list.RemoveFirst();
                        return item.Value;
                    }
                }
            }
            return null;
        }

        public void StatusReceived(TwitterStatus status)
        {
            lock (_lowPriorityQueue)
            {
                _lowPriorityQueue.AddLast(new NotificationData(SlimNotificationKind.New, status));
            }
            OnNewNotificationDataQueued.SafeInvoke();
        }

        public void MentionReceived(TwitterStatus status)
        {
            lock (_highPriorityQueue)
            {
                _highPriorityQueue.AddLast(new NotificationData(SlimNotificationKind.Mention, status));
            }
            OnNewNotificationDataQueued.SafeInvoke();
        }

        public void MessageReceived(TwitterStatus status)
        {
            lock (_urgentPriorityQueue)
            {
                _urgentPriorityQueue.AddLast(new NotificationData(SlimNotificationKind.Message, status));
            }
            OnNewNotificationDataQueued.SafeInvoke();
        }

        public void Followed(TwitterUser source, TwitterUser target)
        {
            lock (_middlePriorityQueue)
            {
                _middlePriorityQueue.AddLast(new NotificationData(SlimNotificationKind.Follow, source, target));
            }
            OnNewNotificationDataQueued.SafeInvoke();
        }

        public void Favorited(TwitterUser source, TwitterStatus target)
        {
            lock (_middlePriorityQueue)
            {
                _middlePriorityQueue.AddLast(new NotificationData(SlimNotificationKind.Favorite, source, target));
            }
            OnNewNotificationDataQueued.SafeInvoke();
        }

        public void Retweeted(TwitterUser source, TwitterStatus original, TwitterStatus retweet)
        {
            lock (_middlePriorityQueue)
            {
                _middlePriorityQueue.AddLast(new NotificationData(SlimNotificationKind.Retweet, source, original));
            }
            OnNewNotificationDataQueued.SafeInvoke();
        }
    }

    public enum SlimNotificationKind
    {
        New,
        Mention,
        Message,
        Follow,
        Favorite,
        Retweet
    }

    public class NotificationData
    {
        public NotificationData(SlimNotificationKind kind, TwitterStatus targetStatus)
        {
            Kind = kind;
            TargetStatus = targetStatus;
        }

        public NotificationData(SlimNotificationKind kind, TwitterUser source, TwitterStatus targetStatus)
        {
            Kind = kind;
            SourceUser = source;
            TargetStatus = targetStatus;
        }

        public NotificationData(SlimNotificationKind kind, TwitterUser source, TwitterUser targetUser)
        {
            Kind = kind;
            SourceUser = source;
            TargetUser = targetUser;
        }

        public SlimNotificationKind Kind { get; }

        public TwitterUser SourceUser { get; }

        public TwitterStatus TargetStatus { get; }

        public TwitterUser TargetUser { get; }
    }
}