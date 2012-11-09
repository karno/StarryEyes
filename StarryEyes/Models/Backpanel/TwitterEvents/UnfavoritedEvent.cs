using System.Windows.Media;
using StarryEyes.Breezy.DataModel;

namespace StarryEyes.Models.Backpanel.TwitterEvents
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
            get { return Source.ScreenName + ": " + TargetStatus.ToString(); }
        }

        public override System.Windows.Media.Color Background
        {
            get{return Colors.DimGray; }
        }
    }
}