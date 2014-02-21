using StarryEyes.Anomaly.TwitterApi.DataModels;

namespace StarryEyes.Views.Notifications
{
    public sealed class NullNotificator : INotificator
    {
        private static readonly NullNotificator _nullNotificator = new NullNotificator();

        public static NullNotificator Instance
        {
            get { return _nullNotificator; }
        }

        private NullNotificator() { }

        public void StatusReceived(TwitterStatus status) { }

        public void MentionReceived(TwitterStatus status) { }

        public void MessageReceived(TwitterStatus status) { }

        public void Followed(TwitterUser source, TwitterUser target) { }

        public void Unfollowed(TwitterUser source, TwitterUser target) { }

        public void Favorited(TwitterUser source, TwitterStatus target) { }

        public void Retweeted(TwitterUser source, TwitterStatus target) { }
    }
}
