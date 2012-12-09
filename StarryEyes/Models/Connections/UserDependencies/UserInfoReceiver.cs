using System;
using StarryEyes.Breezy.Api.Rest;
using StarryEyes.Breezy.Authorize;
using StarryEyes.Models.Stores;
using StarryEyes.Settings;

namespace StarryEyes.Models.Connections.UserDependencies
{
    /// <summary>
    /// Receives user individual information.
    /// </summary>
    public class UserInfoReceiver : PollingConnectionBase
    {
        public UserInfoReceiver(AuthenticateInfo info)
            : base(info) { }

        protected override int IntervalSec
        {
            get { return Setting.RESTReceivePeriod.Value; }
        }

        protected override void DoReceive()
        {
            this.AuthInfo.ShowUser(this.AuthInfo.Id)
                .Subscribe(_ =>
                {
                    this.AuthInfo.UnreliableScreenName = _.ScreenName;
                    this.AuthInfo.UnreliableProfileImageUriString = _.ProfileImageUri.OriginalString;
                    this.AuthInfo.UserInfo = _;
                    UserStore.Store(_);
                });
        }
    }
}
