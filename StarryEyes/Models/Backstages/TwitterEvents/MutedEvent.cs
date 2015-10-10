using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Views;

namespace StarryEyes.Models.Backstages.TwitterEvents
{
    public class MutedEvent : TwitterEventBase
    {
        public MutedEvent(TwitterUser source, TwitterUser target)
            : base(source, target)
        { }

        public override string Title
        {
            get { return "MUTED"; }
        }

        public override string Detail
        {
            get { return Source.ScreenName + " -%-> " + TargetUser.ScreenName; }
        }

        public override System.Windows.Media.Color Background
        {
            get { return MetroColors.Mauve; }
        }
    }
}
