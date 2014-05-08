using System;
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
            this._account = account;
            _reason = reason;
        }

        public override SystemEventKind Kind
        {
            get { return SystemEventKind.Error; }
        }

        public override string Detail
        {
            get
            {
                return String.Format(
                    BackstageResources.UserStreamDisconnectedFormat,
                    "@" + this._account.UnreliableScreenName,
                    _reason);
            }
        }
    }
}
