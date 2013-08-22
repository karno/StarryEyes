using System;
using StarryEyes.Anomaly.TwitterApi.Rest;
using StarryEyes.Helpers;
using StarryEyes.Models.Accounting;
using StarryEyes.Models.Backstages.NotificationEvents;
using StarryEyes.Models.Stores;
using StarryEyes.Settings;

namespace StarryEyes.Models.Receivers.ReceiveElements
{
    public class UserInfoReceiver : CyclicReceiverBase
    {
        private readonly TwitterAccount _account;

        public UserInfoReceiver(TwitterAccount account)
        {
            this._account = account;
        }

        protected override int IntervalSec
        {
            get { return Setting.UserInfoReceivePeriod.Value; }
        }

        protected override async void DoReceive()
        {
            DebugHelper.EnsureBackgroundThread();
            try
            {
                var user = await this._account.ShowUserAsync(this._account.Id);
                this._account.UnreliableScreenName = user.ScreenName;
                this._account.UnreliableProfileImage = user.ProfileImageUri;
                UserStore.Store(user);
            }
            catch (Exception ex)
            {
                BackstageModel.RegisterEvent(new OperationFailedEvent("ユーザ情報を取得できません(@" + this._account.UnreliableScreenName + "): " + ex.Message));
            }
        }
    }
}
