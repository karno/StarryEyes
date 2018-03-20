using System;
using Cadena;
using Cadena.Data;
using JetBrains.Annotations;
using StarryEyes.Feather.Proxies;

namespace StarryEyes.Models.Subsystems.Notifications
{
    public sealed class NotificationProxyWrapper : INotificationSink
    {
        private readonly INotificationProxy _proxy;

        internal INotificationSink Next { get; set; }

        public NotificationProxyWrapper([CanBeNull] INotificationProxy proxy)
        {
            _proxy = proxy ?? throw new ArgumentNullException(nameof(proxy));
        }

        public void NotifyReceived(TwitterStatus status)
        {
            if (!_proxy.NotifyReceived(status))
            {
                Next?.NotifyReceived(status);
            }
        }

        public void NotifyNewArrival(TwitterStatus status, NotificationType type, string explicitSoundSource)
        {
            if (!_proxy.NotifyNewArrival(status, type, explicitSoundSource))
            {
                Next?.NotifyNewArrival(status, type, explicitSoundSource);
            }
        }

        public void NotifyFollowed(TwitterUser source, TwitterUser target)
        {
            if (!_proxy.NotifyFollowed(source, target))
            {
                Next?.NotifyFollowed(source, target);
            }
        }

        public void NotifyUnfollowed(TwitterUser source, TwitterUser target)
        {
            if (!_proxy.NotifyUnfollowed(source, target))
            {
                Next?.NotifyUnfollowed(source, target);
            }
        }

        public void NotifyBlocked(TwitterUser source, TwitterUser target)
        {
            if (!_proxy.NotifyBlocked(source, target))
            {
                Next?.NotifyBlocked(source, target);
            }
        }

        public void NotifyUnblocked(TwitterUser source, TwitterUser target)
        {
            if (!_proxy.NotifyUnblocked(source, target))
            {
                Next?.NotifyUnblocked(source, target);
            }
        }

        public void NotifyMuted(TwitterUser source, TwitterUser target)
        {
            if (!_proxy.NotifyMuted(source, target))
            {
                Next?.NotifyMuted(source, target);
            }
        }

        public void NotifyUnmuted(TwitterUser source, TwitterUser target)
        {
            if (!_proxy.NotifyUnmuted(source, target))
            {
                Next?.NotifyUnmuted(source, target);
            }
        }

        public void NotifyFavorited(TwitterUser source, TwitterStatus status)
        {
            if (!_proxy.NotifyFavorited(source, status))
            {
                Next?.NotifyFavorited(source, status);
            }
        }

        public void NotifyUnfavorited(TwitterUser source, TwitterStatus status)
        {
            if (!_proxy.NotifyUnfavorited(source, status))
            {
                Next?.NotifyUnfavorited(source, status);
            }
        }

        public void NotifyRetweeted(TwitterUser source, TwitterStatus status)
        {
            if (!_proxy.NotifyRetweeted(source, status))
            {
                Next?.NotifyRetweeted(source, status);
            }
        }

        public void NotifyQuoted(TwitterUser source, TwitterStatus original, TwitterStatus quote)
        {
            if (!_proxy.NotifyQuoted(source, original, quote))
            {
                Next?.NotifyQuoted(source, original, quote);
            }
        }

        public void NotifyRetweetFavorited(TwitterUser source, TwitterUser target, TwitterStatus status)
        {
            if (!_proxy.NotifyRetweetFavorited(source, target, status))
            {
                Next?.NotifyRetweetFavorited(source, target, status);
            }
        }

        public void NotifyRetweetRetweeted(TwitterUser source, TwitterUser target, TwitterStatus status)
        {
            if (!_proxy.NotifyRetweetRetweeted(source, target, status))
            {
                Next?.NotifyRetweetRetweeted(source, target, status);
            }
        }


        public void NotifyDeleted(long statusId, TwitterStatus status)
        {
            if (!_proxy.NotifyDeleted(statusId, status))
            {
                Next?.NotifyDeleted(statusId, status);
            }
        }

        public void NotifyLimitationInfoGot(IOAuthCredential account, int trackLimit)
        {
            if (!_proxy.NotifyLimitationInfoGot(account, trackLimit))
            {
                Next?.NotifyLimitationInfoGot(account, trackLimit);
            }
        }

        public void NotifyUserUpdated(TwitterUser source)
        {
            if (!_proxy.NotifyUserUpdated(source))
            {
                Next?.NotifyUserUpdated(source);
            }
        }
    }
}