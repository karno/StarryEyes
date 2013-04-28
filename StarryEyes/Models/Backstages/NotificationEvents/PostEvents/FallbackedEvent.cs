using StarryEyes.Breezy.Authorize;
using StarryEyes.Views;

namespace StarryEyes.Models.Backstages.NotificationEvents.PostEvents
{
    public sealed class FallbackedEvent : BackstageEventBase
    {
        private readonly AuthenticateInfo _authInfo;
        public AuthenticateInfo AuthInfo
        {
            get { return _authInfo; }
        }

        private readonly AuthenticateInfo _fallbackToAuthInfo;
        public AuthenticateInfo FallbackToAuthInfo
        {
            get { return _fallbackToAuthInfo; }
        }


        public FallbackedEvent(AuthenticateInfo authInfo, AuthenticateInfo fallbackTo)
        {
            this._authInfo = authInfo;
            this._fallbackToAuthInfo = fallbackTo;
        }

        public override string Title
        {
            get { return "FALLBACKED"; }
        }

        public override string Detail
        {
            get
            {
                return _authInfo.UnreliableScreenName +
                    " はPOST規制されているため、 " +
                    _fallbackToAuthInfo.UnreliableScreenName +
                    " へフォールバックされました。";
            }
        }

        public override System.Windows.Media.Color Background
        {
            get { return MetroColors.Purple; }
        }
    }
}
