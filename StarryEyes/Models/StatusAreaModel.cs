using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using StarryEyes.Moon.DataModel;
using System.Collections.Concurrent;

namespace StarryEyes.Models
{
    public static class StatusAreaModel
    {
        private static ConcurrentQueue<StatusItem> _notificationQueue = new ConcurrentQueue<StatusItem>();

        public static void EnqueueNotification(StatusItem item)
        {
            _notificationQueue.Enqueue(item);
        }

        public static CompressedStatusItem DequeueNotifications()
        {
            return new CompressedStatusItem();
        }
    }

    public class StatusItem
    {
        public TwitterUser SourceUser { get; set; }

        public TwitterUser TargetUser { get; set; }

        public TwitterStatus TargetTweet { get; set; }

        public StatusActivity Activity { get; set; }
    }

    public class CompressedStatusItem
    {
        public StatusActivity Activity { get; set; }

        public CompressedPropertyKind CompressedKind { get; set; }

        public IEnumerable<TwitterUser> SourceUsers { get; set; }

        public IEnumerable<TwitterUser> TargetUsers { get; set; }

        public IEnumerable<TwitterStatus> TargetTweets { get; set; }
    }

    public enum CompressedPropertyKind
    {
        SourceUsers,
        TargetUsers,
        TargetTweets,
    }

    public enum StatusActivity
    {
        Tweeeted,
        Deleted,
        Favorited,
        Unfavorited,
        Retweeted,
        Fallbacked,
        Followed,
        AppInformation,
        AppWarning,
        AppError,
    }
}
