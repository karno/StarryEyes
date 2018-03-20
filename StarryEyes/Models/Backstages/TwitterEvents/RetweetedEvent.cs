using Cadena.Data;
using StarryEyes.Views;

namespace StarryEyes.Models.Backstages.TwitterEvents
{
    public sealed class RetweetedEvent : TwitterEventBase
    {
        private readonly bool _notifyRetweet;

        public RetweetedEvent(TwitterUser source, TwitterStatus status)
            : base(source, status.User, status)
        {
            _notifyRetweet = false;
        }

        public RetweetedEvent(TwitterUser source, TwitterUser target, TwitterStatus status)
            : base(source, target, status)
        {
            _notifyRetweet = true;
        }

        public override string Title => "RT";

        public override string Detail => _notifyRetweet
            ? Source.ScreenName + ": RT " + TargetUser.ScreenName + ": " + TargetStatus
            : Source.ScreenName + ": " + TargetStatus;

        public override System.Windows.Media.Color Background => MetroColors.Emerald;
    }
}