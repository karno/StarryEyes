using Cadena.Data;

namespace StarryEyes.Models.Backstages.TwitterEvents
{
    public class FollowedEvent : TwitterEventBase
    {
        public FollowedEvent(TwitterUser source, TwitterUser target)
            : base(source, target)
        {
        }

        public override string Title => "FOLLOWED";

        public override string Detail => Source.ScreenName + " -> " + TargetUser.ScreenName;
    }
}