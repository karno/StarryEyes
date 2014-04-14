using System.Threading.Tasks;
using StarryEyes.Anomaly.TwitterApi.Rest;
using StarryEyes.Anomaly.Utils;
using StarryEyes.Models.Accounting;
using StarryEyes.Models.Databases;
using StarryEyes.Settings;

namespace StarryEyes.Models.Receiving.Receivers
{
    public class UserInfoReceiver : CyclicReceiverBase
    {
        private readonly TwitterAccount _account;

        public UserInfoReceiver(TwitterAccount account)
        {
            this._account = account;
        }

        protected override string ReceiverName
        {
            get { return "ユーザー情報(@" + _account.UnreliableScreenName + ")"; }
        }

        protected override int IntervalSec
        {
            get { return Setting.UserInfoReceivePeriod.Value; }
        }

        protected override async Task DoReceive()
        {
            var user = await this._account.ShowUserAsync(this._account.Id);
            this._account.UnreliableScreenName = user.ScreenName;
            this._account.UnreliableProfileImage = user.ProfileImageUri.ChangeImageSize(ImageSize.Original);
            UserProxy.StoreUser(user);
        }
    }
}
