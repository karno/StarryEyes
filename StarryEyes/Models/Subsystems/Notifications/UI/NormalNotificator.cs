using System;
using StarryEyes.Albireo;
using StarryEyes.Albireo.Helpers;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.ViewModels.Notifications;

namespace StarryEyes.Models.Subsystems.Notifications.UI
{
    public class NormalNotificator : IUINotificator
    {
        private static readonly NormalNotificator _instance = new NormalNotificator();

        public static NormalNotificator Instance
        {
            get { return _instance; }
        }

        static NormalNotificator()
        {
            NormalNotificatorViewModel.Initialize();
        }

        public event Action<TwitterStatus> OnStatusReceived;

        public event Action<TwitterStatus> OnMentionReceived;

        public event Action<TwitterStatus> OnMessageReceived;

        public event Action<TwitterUser, TwitterUser> OnUserFollowed;

        public event Action<TwitterUser, TwitterStatus> OnFavorited;

        public event Action<TwitterUser, TwitterStatus> OnRetweeted;

        public void StatusReceived(TwitterStatus status)
        {
            OnStatusReceived.SafeInvoke(status);
        }

        public void MentionReceived(TwitterStatus status)
        {
            OnMentionReceived.SafeInvoke(status);
        }

        public void MessageReceived(TwitterStatus status)
        {
            OnMessageReceived.SafeInvoke(status);
        }

        public void Followed(TwitterUser source, TwitterUser target)
        {
            OnUserFollowed.SafeInvoke(source, target);
        }

        public void Favorited(TwitterUser source, TwitterStatus target)
        {
            OnFavorited.SafeInvoke(source, target);
        }

        public void Retweeted(TwitterUser source, TwitterStatus original, TwitterStatus retweet)
        {
            OnRetweeted.SafeInvoke(source, original);
        }
    }
}
