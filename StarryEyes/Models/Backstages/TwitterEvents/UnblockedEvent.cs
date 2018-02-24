using Cadena.Data;

namespace StarryEyes.Models.Backstages.TwitterEvents
{
    public sealed class UnblockedEvent : TwitterEventBase
    {
        public UnblockedEvent(TwitterUser source, TwitterUser target)
            : base(source, target)
        {
        }

        public override string Title => "UNBLOCKED";

        public override string Detail => Source.ScreenName + " -o-> " + TargetUser.ScreenName;
    }
}