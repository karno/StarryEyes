using System.Windows.Media;
using Cadena.Data;
using StarryEyes.Views;

namespace StarryEyes.Models.Backstages.TwitterEvents
{
    public sealed class FavoritedEvent : TwitterEventBase
    {
        public FavoritedEvent(TwitterUser source, TwitterStatus target)
            : base(source, target)
        {
        }

        public override string Title => "★";

        public override string Detail => Source.ScreenName + ": " + TargetStatus;

        public override Color Background => MetroColors.Amber;
    }
}