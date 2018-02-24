using Cadena.Data;
using StarryEyes.Views;

namespace StarryEyes.Models.Backstages.TwitterEvents
{
    public sealed class BlockedEvent : TwitterEventBase
    {
        public BlockedEvent(TwitterUser source, TwitterUser target)
            : base(source, target)
        {
        }

        public override string Title => "BLOCKED";

        public override string Detail => Source.ScreenName + " -x-> " + TargetUser.ScreenName;

        public override System.Windows.Media.Color Background => MetroColors.Magenta;
    }
}