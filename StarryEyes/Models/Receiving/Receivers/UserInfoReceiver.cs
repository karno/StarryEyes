using System.Threading.Tasks;
using StarryEyes.Anomaly.TwitterApi.Rest;
using StarryEyes.Anomaly.Utils;
using StarryEyes.Globalization;
using StarryEyes.Globalization.Models;
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
            _account = account;
        }

        protected override string ReceiverName
        {
            get { return ReceivingResources.ReceiverUserInfoFormat.SafeFormat("@" + _account.UnreliableScreenName); }
        }

        protected override int IntervalSec
        {
            get { return Setting.UserInfoReceivePeriod.Value; }
        }

        protected override async Task DoReceive()
        {
            var user = await _account.ShowUserAsync(_account.Id).ConfigureAwait(false);
            _account.UnreliableScreenName = user.ScreenName;
            _account.UnreliableProfileImage = user.ProfileImageUri.ChangeImageSize(ImageSize.Original);
            UserProxy.StoreUser(user);
        }
    }
}
