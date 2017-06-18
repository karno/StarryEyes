using System.ComponentModel;
using StarryEyes.Anomaly;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Feather.Injections;

namespace StarryEyes.Feather.Proxies
{
    /// <summary>
    /// Notification proxy.
    /// </summary>
    public static class NotificationProxy
    {
        /// <summary>
        /// For internal framework.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly BridgeSocket<INotificationProxy> Socket = new BridgeSocket<INotificationProxy>();

        /// <summary>
        /// Register new proxy.
        /// </summary>
        /// <param name="proxy"></param>
        public static void Register(INotificationProxy proxy)
        {
            Socket.Call(proxy);
        }
    }

    /// <summary>
    /// Notification proxy
    /// </summary>
    public interface INotificationProxy
    {
        /// <summary>
        /// New status is received.
        /// </summary>
        /// <param name="status">received status</param>
        /// <returns>return true to trap notification</returns>
        bool NotifyReceived(TwitterStatus status);

        /// <summary>
        /// Notification is required by tabs.
        /// </summary>
        /// <param name="status">target status</param>
        /// <param name="type">notification type</param>
        /// <param name="explicitSoundSource">specified sound source, can be null.</param>
        /// <returns>return true to trap notification</returns>
        bool NotifyNewArrival(TwitterStatus status, NotificationType type, string explicitSoundSource);

        /// <summary>
        /// New user started following someone.
        /// </summary>
        /// <param name="source">source user</param>
        /// <param name="target">target user</param>
        /// <returns>return true to trap notification</returns>
        bool NotifyFollowed(TwitterUser source, TwitterUser target);

        /// <summary>
        /// The user cancelled following someone.
        /// </summary>
        /// <param name="source">source user</param>
        /// <param name="target">target user</param>
        /// <returns>return true to trap notification</returns>
        bool NotifyUnfollowed(TwitterUser source, TwitterUser target);

        /// <summary>
        /// The user blocked someone.
        /// </summary>
        /// <param name="source">source user</param>
        /// <param name="target">target user</param>
        /// <returns>return true to trap notification</returns>
        bool NotifyBlocked(TwitterUser source, TwitterUser target);

        /// <summary>
        /// The user cancelled blocking someone.
        /// </summary>
        /// <param name="source">source user</param>
        /// <param name="target">target user</param>
        /// <returns>return true to trap notification</returns>
        bool NotifyUnblocked(TwitterUser source, TwitterUser target);

        /// <summary>
        /// The user muted someone.
        /// </summary>
        /// <param name="source">source user</param>
        /// <param name="target">target user</param>
        /// <returns>return true to trap notification</returns>
        bool NotifyMuted(TwitterUser source, TwitterUser target);

        /// <summary>
        /// The user cancelled muting someone.
        /// </summary>
        /// <param name="source">source user</param>
        /// <param name="target">target user</param>
        /// <returns>return true to trap notification</returns>
        bool NotifyUnmuted(TwitterUser source, TwitterUser target);

        /// <summary>
        /// The user favorited the tweet.
        /// </summary>
        /// <param name="source">source user</param>
        /// <param name="status">target status</param>
        /// <returns>return true to trap notification</returns>
        bool NotifyFavorited(TwitterUser source, TwitterStatus status);

        /// <summary>
        /// The user unfavorited the tweet.
        /// </summary>
        /// <param name="source">source user</param>
        /// <param name="status">target status</param>
        /// <returns>return true to trap notification</returns>
        bool NotifyUnfavorited(TwitterUser source, TwitterStatus status);

        /// <summary>
        /// The user retweeted the tweet.
        /// </summary>
        /// <param name="source">source user</param>
        /// <param name="original">original tweet</param>
        /// <param name="retweet">retweeted tweet</param>
        /// <returns>return true to trap notification</returns>
        bool NotifyRetweeted(TwitterUser source, TwitterStatus original, TwitterStatus retweet);

        /// <summary>
        /// The status is deleted.
        /// </summary>
        /// <param name="statusId">deleted status Id</param>
        /// <param name="deleted">deleted status object, can be null.</param>
        /// <returns>return true to trap notification</returns>
        bool NotifyDeleted(long statusId, TwitterStatus deleted);

        /// <summary>
        /// Got a track limit notification.
        /// </summary>
        /// <param name="account">target account</param>
        /// <param name="trackLimit">droppped status count</param>
        /// <returns>return true to trap notification</returns>
        bool NotifyLimitationInfoGot(IOAuthCredential account, int trackLimit);

        /// <summary>
        /// User information updated.
        /// </summary>
        /// <param name="source">updated user info</param>
        /// <returns>return true to trap notification</returns>
        bool NotifyUserUpdated(TwitterUser source);
    }

    public enum NotificationType
    {
        /// <summary>
        /// Dispatch notification as normal status.
        /// </summary>
        Normal,

        /// <summary>
        /// Dispatch notification as status mention to me.
        /// </summary>
        Mention,

        /// <summary>
        /// Dispatch notification as private message.
        /// </summary>
        DirectMessage,
    }
}