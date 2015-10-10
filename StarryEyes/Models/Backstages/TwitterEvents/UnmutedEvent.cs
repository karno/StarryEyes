using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Views;

namespace StarryEyes.Models.Backstages.TwitterEvents
{
    public class UnmutedEvent : TwitterEventBase
    {
        public UnmutedEvent(TwitterUser source, TwitterUser target)
            : base(source, target)
        { }

        public override string Title
        {
            get { return "UNMUTED"; }
        }

        public override string Detail
        {
            get { return Source.ScreenName + " -o-> " + TargetUser.ScreenName; }
        }

        public override System.Windows.Media.Color Background
        {
            get { return MetroColors.Mauve; }
        }
    }
}

