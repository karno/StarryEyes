using StarryEyes.Breezy.Authorize;

namespace StarryEyes.Models.Backstages.SystemEvents
{
    public sealed class UserStreamsDisconnectedEvent : SystemEventBase
    {
        private readonly AuthenticateInfo _info;
        private readonly string _reason;

        public UserStreamsDisconnectedEvent(AuthenticateInfo info, string reason)
        {
            _info = info;
            _reason = reason;
        }

        public override SystemEventKind Kind
        {
            get { return SystemEventKind.Error; }
        }

        public override string Detail
        {
            get { return "User Streamsが切断されました: " + _info.UnreliableScreenName + ", " + _reason; }
        }
    }
}
