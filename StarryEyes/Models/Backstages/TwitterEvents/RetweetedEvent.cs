using Cadena.Data;
using StarryEyes.Views;

namespace StarryEyes.Models.Backstages.TwitterEvents
{
    public sealed class RetweetedEvent : TwitterEventBase
    {
        public RetweetedEvent(TwitterUser source, TwitterStatus target)
            : base(source, target)
        {
        }

        public override string Title => "RT";

        public override string Detail => Source.ScreenName + ": " + TargetStatus;

        public override System.Windows.Media.Color Background => MetroColors.Emerald;
    }
}