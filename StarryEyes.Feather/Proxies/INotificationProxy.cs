using StarryEyes.Anomaly;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Feather.Injections;

namespace StarryEyes.Feather.Proxies
{
    public static class NotificationProxy
    {
        public static readonly BridgeSocket<INotificationProxy> Socket = new BridgeSocket<INotificationProxy>();

        public static void Register(INotificationProxy proxy)
        {
            Socket.Call(proxy);
        }
    }

    public interface INotificationProxy
    {
        bool NotifyReceived(TwitterStatus status);

        bool NotifyNewArrival(TwitterStatus status, NotificationType type, string explicitSoundSource);

        bool NotifyFollowed(TwitterUser source, TwitterUser target);

        bool NotifyUnfollowed(TwitterUser source, TwitterUser target);

        bool NotifyBlocked(TwitterUser source, TwitterUser target);

        bool NotifyUnblocked(TwitterUser source, TwitterUser target);

        bool NotifyFavorited(TwitterUser source, TwitterStatus status);

        bool NotifyUnfavorited(TwitterUser source, TwitterStatus status);

        bool NotifyRetweeted(TwitterUser source, TwitterStatus status);

        bool NotifyDeleted(long statusId, TwitterStatus deleted);

        bool NotifyLimitationInfoGot(IOAuthCredential account, int trackLimit);

        bool NotifyUserUpdated(TwitterUser source);
    }

    public enum NotificationType
    {
        Normal,
        Mention,
        DirectMessage,
    }
}
