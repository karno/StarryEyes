using System;
using Cadena.Data;
using StarryEyes.ViewModels.Notifications;

namespace StarryEyes.Models.Subsystems.Notifications.UI
{
    public class NormalNotificator : IUINotificator
    {
        public static NormalNotificator Instance { get; } = new NormalNotificator();

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

        public event Action<TwitterUser, TwitterUser, TwitterStatus> OnRetweetFavorited;

        public event Action<TwitterUser, TwitterUser, TwitterStatus> OnRetweetRetweeted;

        public event Action<TwitterUser, TwitterStatus> OnQuoted;

        public void StatusReceived(TwitterStatus status)
        {
            OnStatusReceived?.Invoke(status);
        }

        public void MentionReceived(TwitterStatus status)
        {
            OnMentionReceived?.Invoke(status);
        }

        public void MessageReceived(TwitterStatus status)
        {
            OnMessageReceived?.Invoke(status);
        }

        public void Followed(TwitterUser source, TwitterUser target)
        {
            OnUserFollowed?.Invoke(source, target);
        }

        public void Favorited(TwitterUser source, TwitterStatus target)
        {
            OnFavorited?.Invoke(source, target);
        }

        public void Retweeted(TwitterUser source, TwitterStatus target)
        {
            OnRetweeted?.Invoke(source, target);
        }

        public void Quoted(TwitterUser source, TwitterStatus original, TwitterStatus quote)
        {
            OnQuoted?.Invoke(source, quote);
        }

        public void RetweetFavorited(TwitterUser source, TwitterUser target, TwitterStatus status)
        {
            OnRetweetFavorited?.Invoke(source, target, status);
        }

        public void RetweetRetweeted(TwitterUser source, TwitterUser target, TwitterStatus status)
        {
            OnRetweetRetweeted?.Invoke(source, target, status);
        }
    }
}