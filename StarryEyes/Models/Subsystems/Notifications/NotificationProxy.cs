using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Models.Accounting;

namespace StarryEyes.Models.Subsystems.Notifications
{
    public abstract class NotificationProxy : INotificationSink
    {
        internal INotificationSink Next { get; set; }

        public virtual void NotifyReceived(TwitterStatus status)
        {
            if (Next != null)
            {
                Next.NotifyReceived(status);
            }
        }

        public void NotifyNewArrival(TwitterStatus status, NotificationType type, string explicitSoundSource)
        {
            if (Next != null)
            {
                Next.NotifyNewArrival(status, type, explicitSoundSource);
            }
        }

        public void NotifyFollowed(TwitterUser source, TwitterUser target)
        {
            if (Next != null)
            {
                Next.NotifyFollowed(source, target);
            }
        }

        public void NotifyUnfollowed(TwitterUser source, TwitterUser target)
        {
            if (Next != null)
            {
                Next.NotifyUnfollowed(source, target);
            }
        }

        public void NotifyBlocked(TwitterUser source, TwitterUser target)
        {
            if (Next != null)
            {
                Next.NotifyBlocked(source, target);
            }
        }

        public void NotifyUnblocked(TwitterUser source, TwitterUser target)
        {
            if (Next != null)
            {
                Next.NotifyUnblocked(source, target);
            }
        }

        public void NotifyFavorited(TwitterUser source, TwitterStatus status)
        {
            if (Next != null)
            {
                Next.NotifyFavorited(source, status);
            }
        }

        public void NotifyUnfavorited(TwitterUser source, TwitterStatus status)
        {
            if (Next != null)
            {
                Next.NotifyUnfavorited(source, status);
            }
        }

        public void NotifyRetweeted(TwitterUser source, TwitterStatus status)
        {
            if (Next != null)
            {
                Next.NotifyRetweeted(source, status);
            }
        }

        public void NotifyDeleted(long statusId, TwitterStatus deleted)
        {
            if (Next != null)
            {
                Next.NotifyDeleted(statusId, deleted);
            }
        }

        public void NotifyLimitationInfoGot(TwitterAccount account, int trackLimit)
        {
            if (Next != null)
            {
                Next.NotifyLimitationInfoGot(account, trackLimit);
            }
        }

        public void NotifyUserUpdated(TwitterUser source)
        {
            if (Next != null)
            {
                Next.NotifyUserUpdated(source);
            }
        }
    }
}
