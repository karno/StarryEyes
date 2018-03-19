using Cadena;
using Cadena.Data;
using StarryEyes.Feather.Proxies;
using StarryEyes.Models.Accounting;
using StarryEyes.Models.Backstages.NotificationEvents;
using StarryEyes.Models.Backstages.TwitterEvents;
using StarryEyes.Settings;

namespace StarryEyes.Models.Subsystems.Notifications
{
    internal sealed class NotificationSink : INotificationSink
    {
        public void NotifyReceived(TwitterStatus status)
        {
            // do nothing.
        }

        public void NotifyNewArrival(TwitterStatus status, NotificationType type, string explicitSoundSource)
        {
            // TODO: implementation
        }

        public void NotifyFollowed(TwitterUser source, TwitterUser target)
        {
            BackstageModel.RegisterEvent(new FollowedEvent(source, target));
        }

        public void NotifyUnfollowed(TwitterUser source, TwitterUser target)
        {
            BackstageModel.RegisterEvent(new UnfollowedEvent(source, target));
        }

        public void NotifyBlocked(TwitterUser source, TwitterUser target)
        {
            BackstageModel.RegisterEvent(new BlockedEvent(source, target));
        }

        public void NotifyUnblocked(TwitterUser source, TwitterUser target)
        {
            BackstageModel.RegisterEvent(new UnblockedEvent(source, target));
        }

        public void NotifyMuted(TwitterUser source, TwitterUser target)
        {
            BackstageModel.RegisterEvent(new MutedEvent(source, target));
        }

        public void NotifyUnmuted(TwitterUser source, TwitterUser target)
        {
            BackstageModel.RegisterEvent(new UnmutedEvent(source, target));
        }

        public void NotifyFavorited(TwitterUser source, TwitterStatus status)
        {
            BackstageModel.RegisterEvent(new FavoritedEvent(source, status));
        }

        public void NotifyUnfavorited(TwitterUser source, TwitterStatus status)
        {
            BackstageModel.RegisterEvent(new UnfavoritedEvent(source, status));
        }

        public void NotifyRetweeted(TwitterUser source, TwitterStatus original, TwitterStatus retweet,
            bool retweetedRetweet)
        {
            if (retweetedRetweet ||
                (retweet.RetweetedStatus != null && (
                     Setting.Accounts.Contains(source.Id) ||
                     Setting.Accounts.Contains(original.User.Id) ||
                     Setting.Accounts.Contains(retweet.User.Id))))
            {
                BackstageModel.RegisterEvent(new RetweetedEvent(source, original));
            }
        }

        public void NotifyQuoted(TwitterUser source, TwitterStatus original, TwitterStatus quote)
        {
            if (quote.QuotedStatus != null && (
                    Setting.Accounts.Contains(source.Id) ||
                    Setting.Accounts.Contains(original.User.Id)))
            {
                BackstageModel.RegisterEvent(new QuotedEvent(source, quote));
            }
        }

        public void NotifyDeleted(long statusId, TwitterStatus deleted)
        {
            // do nothing, currently.
        }

        public void NotifyLimitationInfoGot(IOAuthCredential account, int trackLimit)
        {
            var acc = account as TwitterAccount;
            if (acc == null) return;
            BackstageModel.RegisterEvent(new TrackLimitEvent(acc, trackLimit));
        }

        public void NotifyUserUpdated(TwitterUser source)
        {
            // do nothing, currently.
        }
    }
}