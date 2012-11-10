using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using StarryEyes.Breezy.Authorize;
using StarryEyes.Views;

namespace StarryEyes.Models.Backpanels.PostEvents
{
    public sealed class FallbackedEvent : BackpanelEventBase
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
                    " is post limited. fallback to " +
                    _fallbackToAuthInfo.UnreliableScreenName;
            }
        }

        public override System.Windows.Media.Color Background
        {
            get { return MetroColors.Purple; }
        }
    }
}
