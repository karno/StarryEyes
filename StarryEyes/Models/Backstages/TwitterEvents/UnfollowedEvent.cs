using System.Windows.Media;
using Cadena.Data;

namespace StarryEyes.Models.Backstages.TwitterEvents
{
    public sealed class UnfollowedEvent : TwitterEventBase
    {
        public UnfollowedEvent(TwitterUser source, TwitterUser target)
            : base(source, target)
        {
        }

        public override string Title => "UNFOLLOWED";

        public override string Detail => Source.ScreenName + " -/-> " + TargetUser.ScreenName;

        public override Color Background => Colors.DimGray;
    }
}