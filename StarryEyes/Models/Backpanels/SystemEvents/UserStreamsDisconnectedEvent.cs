using System;
using StarryEyes.Breezy.Authorize;

namespace StarryEyes.Models.Backpanels.SystemEvents
{
    public sealed class UserStreamsDisconnectedEvent : SystemEventBase
    {
        private readonly AuthenticateInfo _info;
        private readonly string _reason;
        private readonly Action _reconnect;

        public UserStreamsDisconnectedEvent(AuthenticateInfo info, string reason, Action reconnect)
        {
            _info = info;
            _reason = reason;
            _reconnect = reconnect;
        }

        public override SystemEventKind Kind
        {
            get { return SystemEventKind.Error; }
        }

        public override string Detail
        {
            get { return "User Streamsが切断されました: " + _reason; }
        }

        public override string Id
        {
            get { return "USERSTREAMS_DISCONNECTED_" + _info.UnreliableScreenName; }
        }

        public override SystemEventAction Action
        {
            get
            {
                return new SystemEventAction("再接続", _reconnect);
            }
        }
    }
}
