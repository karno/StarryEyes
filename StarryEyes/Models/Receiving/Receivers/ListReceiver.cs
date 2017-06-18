using System.Linq;
using System.Threading.Tasks;
using StarryEyes.Anomaly.TwitterApi.Rest;
using StarryEyes.Globalization;
using StarryEyes.Globalization.Models;
using StarryEyes.Models.Accounting;
using StarryEyes.Models.Backstages.NotificationEvents;
using StarryEyes.Models.Receiving.Handling;
using StarryEyes.Settings;

namespace StarryEyes.Models.Receiving.Receivers
{
    public sealed class ListReceiver : CyclicReceiverBase
    {
        private readonly ListInfo _listInfo;
        private readonly TwitterAccount _auth;

        public ListReceiver(ListInfo listInfo)
        {
            this._listInfo = listInfo;
        }

        public ListReceiver(TwitterAccount auth, ListInfo listInfo)
        {
            this._auth = auth;
            this._listInfo = listInfo;
        }

        protected override string ReceiverName
        {
            get { return ReceivingResources.ReceiverListTimelineFormat.SafeFormat(_listInfo); }
        }

        protected override int IntervalSec
        {
            get { return Setting.ListReceivePeriod.Value; }
        }

        protected override async Task DoReceive()
        {
            var authInfo = this._auth ?? Setting.Accounts.GetRandomOne();
            if (authInfo == null)
            {
                BackstageModel.RegisterEvent(new OperationFailedEvent(
                    ReceivingResources.AccountIsNotRegisteredForList, null));
                return;
            }

            (await authInfo.GetListTimelineAsync(this._listInfo.Slug, this._listInfo.OwnerScreenName))
                .ForEach(StatusInbox.Enqueue);
        }
    }
}
