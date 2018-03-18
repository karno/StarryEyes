using StarryEyes.Globalization;
using StarryEyes.Models.Accounting;

namespace StarryEyes.Models.Backstages.SystemEvents
{
    public sealed class UserStreamsDisconnectedEvent : SystemEventBase
    {
        private readonly TwitterAccount _account;
        private readonly string _reason;

        public UserStreamsDisconnectedEvent(TwitterAccount account, string reason)
        {
            _account = account;
            _reason = reason;
        }

        public override SystemEventKind Kind => SystemEventKind.Error;

        public override string Detail => BackstageResources.UserStreamDisconnectedFormat.SafeFormat(
            "@" + _account.UnreliableScreenName, _reason);
    }
}