using System.Windows.Media;
using StarryEyes.Breezy.DataModel;

namespace StarryEyes.Models.Backpanels.TwitterEvents
{
    public sealed class UnfollowedEvent : TwitterEventBase
    {
        public UnfollowedEvent(TwitterUser source, TwitterUser target)
            : base(source, target) { }

        public override string Title
        {
            get { return "UNFOLLOWED"; }
        }

        public override string Detail
        {
            get { return Source.ScreenName + " -/-> " + TargetUser.ScreenName; }
        }

        public override System.Windows.Media.Color Background
        {
            get
            {
                return Colors.DimGray;
            }
        }
    }
}
