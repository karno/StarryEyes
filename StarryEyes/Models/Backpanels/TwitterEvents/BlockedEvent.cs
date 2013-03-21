using StarryEyes.Breezy.DataModel;
using StarryEyes.Views;

namespace StarryEyes.Models.Backpanels.TwitterEvents
{
    public sealed class BlockedEvent : TwitterEventBase
    {
        public BlockedEvent(TwitterUser source, TwitterUser target)
            : base(source, target) { }

        public override string Title
        {
            get { return "BLOCKED"; }
        }

        public override string Detail
        {
            get { return Source.ScreenName + " -x-> " + TargetUser.ScreenName; }
        }

        public override System.Windows.Media.Color Background
        {
            get { return MetroColors.Magenta; }
        }
    }
}
