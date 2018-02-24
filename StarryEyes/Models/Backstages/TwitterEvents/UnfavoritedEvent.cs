using System.Windows.Media;
using Cadena.Data;

namespace StarryEyes.Models.Backstages.TwitterEvents
{
    public sealed class UnfavoritedEvent : TwitterEventBase
    {
        public UnfavoritedEvent(TwitterUser user, TwitterStatus target)
            : base(user, target)
        {
        }

        public override string Title => "☆";

        public override string Detail => Source.ScreenName + ": " + TargetStatus;

        public override Color Background => Colors.DimGray;
    }
}