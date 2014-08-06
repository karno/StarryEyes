using System;
using JetBrains.Annotations;
using StarryEyes.Anomaly;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Fragments.Proxies;

namespace StarryEyes.Models.Subsystems.Notifications
{
    public sealed class NotificationProxyWrapper : INotificationSink
    {
        private readonly INotificationProxy _proxy;

        internal INotificationSink Next { get; set; }

        public NotificationProxyWrapper([NotNull] INotificationProxy proxy)
        {
            if (proxy == null) throw new ArgumentNullException("proxy");
            this._proxy = proxy;
        }

        public void NotifyReceived(TwitterStatus status)
        {
            if (!_proxy.NotifyReceived(status) && this.Next != null)
            {
                this.Next.NotifyReceived(status);
            }
        }

        public void NotifyNewArrival(TwitterStatus status, NotificationType type, string explicitSoundSource)
        {
            if (!_proxy.NotifyNewArrival(status, type, explicitSoundSource) && this.Next != null)
            {
                this.Next.NotifyNewArrival(status, type, explicitSoundSource);
            }
        }

        public void NotifyFollowed(TwitterUser source, TwitterUser target)
        {
            if (!_proxy.NotifyFollowed(source, target) && this.Next != null)
            {
                this.Next.NotifyFollowed(source, target);
            }
        }

        public void NotifyUnfollowed(TwitterUser source, TwitterUser target)
        {
            if (!_proxy.NotifyUnfollowed(source, target) && this.Next != null)
            {
                this.Next.NotifyUnfollowed(source, target);
            }
        }

        public void NotifyBlocked(TwitterUser source, TwitterUser target)
        {
            if (!_proxy.NotifyBlocked(source, target) && this.Next != null)
            {
                this.Next.NotifyBlocked(source, target);
            }
        }

        public void NotifyUnblocked(TwitterUser source, TwitterUser target)
        {
            if (!_proxy.NotifyUnblocked(source, target) && this.Next != null)
            {
                this.Next.NotifyUnblocked(source, target);
            }
        }

        public void NotifyFavorited(TwitterUser source, TwitterStatus status)
        {
            if (!_proxy.NotifyFavorited(source, status) && this.Next != null)
            {
                this.Next.NotifyFavorited(source, status);
            }
        }

        public void NotifyUnfavorited(TwitterUser source, TwitterStatus status)
        {
            if (!_proxy.NotifyUnfavorited(source, status) && this.Next != null)
            {
                this.Next.NotifyUnfavorited(source, status);
            }
        }

        public void NotifyRetweeted(TwitterUser source, TwitterStatus original, TwitterStatus retweet)
        {
            if (!_proxy.NotifyRetweeted(source, original, retweet) && this.Next != null)
            {
                this.Next.NotifyRetweeted(source, original, retweet);
            }
        }

        public void NotifyDeleted(long statusId, TwitterStatus deleted)
        {
            if (!_proxy.NotifyDeleted(statusId, deleted) && this.Next != null)
            {
                this.Next.NotifyDeleted(statusId, deleted);
            }
        }

        public void NotifyLimitationInfoGot(IOAuthCredential account, int trackLimit)
        {
            if (!_proxy.NotifyLimitationInfoGot(account, trackLimit) && this.Next != null)
            {
                this.Next.NotifyLimitationInfoGot(account, trackLimit);
            }
        }

        public void NotifyUserUpdated(TwitterUser source)
        {
            if (!_proxy.NotifyUserUpdated(source) && this.Next != null)
            {
                this.Next.NotifyUserUpdated(source);
            }
        }
    }
}
