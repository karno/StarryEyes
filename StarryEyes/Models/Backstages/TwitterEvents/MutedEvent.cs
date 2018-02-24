using Cadena.Data;
using StarryEyes.Views;

namespace StarryEyes.Models.Backstages.TwitterEvents
{
    public class MutedEvent : TwitterEventBase
    {
        public MutedEvent(TwitterUser source, TwitterUser target)
            : base(source, target)
        {
        }

        public override string Title => "MUTED";

        public override string Detail => Source.ScreenName + " -%-> " + TargetUser.ScreenName;

        public override System.Windows.Media.Color Background => MetroColors.Mauve;
    }
}