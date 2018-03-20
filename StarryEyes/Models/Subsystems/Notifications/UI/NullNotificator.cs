using Cadena.Data;

namespace StarryEyes.Models.Subsystems.Notifications.UI
{
    public sealed class NullNotificator : IUINotificator
    {
        public static NullNotificator Instance { get; } = new NullNotificator();

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

        public void Retweeted(TwitterUser source, TwitterStatus target)
        {
        }

        public void Quoted(TwitterUser source, TwitterStatus original, TwitterStatus quote)
        {
        }

        public void RetweetFavorited(TwitterUser source, TwitterUser target, TwitterStatus status)
        {
        }

        public void RetweetRetweeted(TwitterUser source, TwitterUser target, TwitterStatus status)
        {
        }
    }
}