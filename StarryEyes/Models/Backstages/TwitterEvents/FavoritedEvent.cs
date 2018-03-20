using System.Windows.Media;
using Cadena.Data;
using StarryEyes.Views;

namespace StarryEyes.Models.Backstages.TwitterEvents
{
    public sealed class FavoritedEvent : TwitterEventBase
    {
        private readonly bool _notifyRetweet;

        public FavoritedEvent(TwitterUser source, TwitterStatus status)
            : base(source, status.User, status)
        {
            _notifyRetweet = false;
        }

        public FavoritedEvent(TwitterUser source, TwitterUser target, TwitterStatus status)
            : base(source, target, status)
        {
            _notifyRetweet = true;
        }

        public override string Title => "★";

        public override string Detail => _notifyRetweet
            ? Source.ScreenName + ": RT " + TargetUser.ScreenName + ": " + TargetStatus
            : Source.ScreenName + ": " + TargetStatus;

        public override Color Background => MetroColors.Amber;
    }
}