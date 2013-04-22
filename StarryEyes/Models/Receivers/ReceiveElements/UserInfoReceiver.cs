using System;
using StarryEyes.Breezy.Api.Rest;
using StarryEyes.Breezy.Authorize;
using StarryEyes.Models.Backpanels.NotificationEvents;
using StarryEyes.Models.Stores;
using StarryEyes.Settings;

namespace StarryEyes.Models.Receivers.ReceiveElements
{
    public class UserInfoReceiver : CyclicReceiverBase
    {
        private readonly AuthenticateInfo _authInfo;

        public UserInfoReceiver(AuthenticateInfo authInfo)
        {
            _authInfo = authInfo;
        }

        protected override int IntervalSec
        {
            get { return Setting.UserInfoReceivePeriod.Value; }
        }

        protected override void DoReceive()
        {
            _authInfo.ShowUser(this._authInfo.Id)
                     .Subscribe(u =>
                     {
                         _authInfo.UnreliableScreenName = u.ScreenName;
                         _authInfo.UnreliableProfileImageUriString = u.ProfileImageUri.OriginalString;
                         _authInfo.UserInfo = u;
                         UserStore.Store(u);
                     },
                                ex => BackpanelModel.RegisterEvent(
                                    new OperationFailedEvent("user info receive error: " +
                                                             _authInfo.UnreliableScreenName + " - " +
                                                             ex.Message)));
        }
    }
}
