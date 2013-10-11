using StarryEyes.Anomaly.TwitterApi.DataModels;
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

        public void NotifyFavorited(TwitterUser source, TwitterStatus status)
        {
            BackstageModel.RegisterEvent(new FavoritedEvent(source, status));
        }

        public void NotifyUnfavorited(TwitterUser source, TwitterStatus status)
        {
            BackstageModel.RegisterEvent(new UnfavoritedEvent(source, status));
        }

        public void NotifyRetweeted(TwitterUser source, TwitterStatus status)
        {
            if (Setting.Accounts.Contains(source.Id) ||
                Setting.Accounts.Contains(status.User.Id))
            {
                BackstageModel.RegisterEvent(new RetweetedEvent(source, status));
            }
        }

        public void NotifyDeleted(TwitterStatus deleted)
        {
            // do nothing, currently.
        }

        public void NotifyLimitationInfoGot(TwitterAccount account, int trackLimit)
        {
            BackstageModel.RegisterEvent(new TrackLimitEvent(account, trackLimit));
        }

        public void NotifyUserUpdated(TwitterUser source)
        {
            // do nothing, currently.
        }
    }
}
