using StarryEyes.Anomaly.TwitterApi.DataModels;

namespace StarryEyes.Models.Subsystems.Notifications.UI
{
    public interface IUINotificator
    {
        void StatusReceived(TwitterStatus status);

        void MentionReceived(TwitterStatus status);

        void MessageReceived(TwitterStatus status);

        void Followed(TwitterUser source, TwitterUser target);

        void Favorited(TwitterUser source, TwitterStatus target);

        void Retweeted(TwitterUser source, TwitterStatus original, TwitterStatus retweet);
    }
}
