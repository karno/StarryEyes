using System.Windows.Media;
using StarryEyes.Breezy.DataModel;

namespace StarryEyes.Models.Backpanels.TwitterEvents
{
    public sealed class UnfavoritedEvent : TwitterEventBase
    {
        public UnfavoritedEvent(TwitterUser user, TwitterStatus target)
            : base(user, target) { }

        public override string Title
        {
            get { return "☆"; }
        }

        public override string Detail
        {
            get { return Source.ScreenName + ": " + TargetStatus; }
        }

        public override Color Background
        {
            get { return Colors.DimGray; }
        }
    }
}