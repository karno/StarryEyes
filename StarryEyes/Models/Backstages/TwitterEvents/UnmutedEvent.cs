using Cadena.Data;
using StarryEyes.Views;

namespace StarryEyes.Models.Backstages.TwitterEvents
{
    public class UnmutedEvent : TwitterEventBase
    {
        public UnmutedEvent(TwitterUser source, TwitterUser target)
            : base(source, target)
        {
        }

        public override string Title => "UNMUTED";

        public override string Detail => Source.ScreenName + " -o-> " + TargetUser.ScreenName;

        public override System.Windows.Media.Color Background => MetroColors.Mauve;
    }
}