using Cadena.Data;

namespace StarryEyes.Models.Subsystems.Notifications.UI
{
    public sealed class NullNotificator : IUINotificator
    {
        private static readonly NullNotificator _nullNotificator = new NullNotificator();

        public static NullNotificator Instance => _nullNotificator;

        private NullNotificator()
        {
        }

        public void StatusReceived(TwitterStatus status)
        {
        }

        public void MentionReceived(TwitterStatus status)
        {
        }

        public void MessageReceived(TwitterStatus status)
        {
        }

        public void Followed(TwitterUser source, TwitterUser target)
        {
        }

        public void Favorited(TwitterUser source, TwitterStatus target)
        {
        }

        public void Retweeted(TwitterUser source, TwitterStatus original, TwitterStatus retweet)
        {
        }

        public void Quoted(TwitterUser source, TwitterStatus original, TwitterStatus quote)
        {
        }
    }
}