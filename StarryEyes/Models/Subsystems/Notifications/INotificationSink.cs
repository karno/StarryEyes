using StarryEyes.Anomaly;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Feather.Proxies;

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

        void NotifyDeleted(long statusId, TwitterStatus deleted);

        void NotifyLimitationInfoGot(IOAuthCredential account, int trackLimit);

        void NotifyUserUpdated(TwitterUser source);
    }

}
