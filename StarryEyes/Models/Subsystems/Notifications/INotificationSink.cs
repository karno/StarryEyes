using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Models.Accounting;

namespace StarryEyes.Models.Subsystems.Notifications
{
    public interface INotificationSink
    {
        void NotifyReceived(TwitterStatus status);

        void NotifyNewArrival(TwitterStatus status, NotificationType type, string explicitSoundSource);

        void NotifyFollowed(TwitterUser source, TwitterUser target);

        void NotifyUnfollowed(TwitterUser source, TwitterUser target);

        void NotifyBlocked(TwitterUser source, TwitterUser target);

        void NotifyUnblocked(TwitterUser source, TwitterUser target);

        void NotifyFavorited(TwitterUser source, TwitterStatus status);

        void NotifyUnfavorited(TwitterUser source, TwitterStatus status);

        void NotifyRetweeted(TwitterUser source, TwitterStatus status);

        void NotifyDeleted(TwitterStatus deleted);

        void NotifyLimitationInfoGot(TwitterAccount account, int trackLimit);

        void NotifyUserUpdated(TwitterUser source);
    }

    public enum NotificationType
    {
        Normal,
        Mention,
        DirectMessage,
    }
}
