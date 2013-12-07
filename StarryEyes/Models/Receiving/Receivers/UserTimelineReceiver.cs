using System;
using System.Linq;
using StarryEyes.Anomaly.TwitterApi.Rest;
using StarryEyes.Models.Accounting;
using StarryEyes.Models.Backstages.NotificationEvents;
using StarryEyes.Models.Receiving.Handling;
using StarryEyes.Settings;

namespace StarryEyes.Models.Receiving.Receivers
{
    public class UserTimelineReceiver : CyclicReceiverBase
    {
        private readonly TwitterAccount _account;

        public UserTimelineReceiver(TwitterAccount account)
        {
            this._account = account;
        }

        protected override string ReceiverName
        {
            get { return "ユーザータイムライン(@" + _account.UnreliableScreenName + ")"; }
        }

        protected override int IntervalSec
        {
            get { return Setting.RESTReceivePeriod.Value; }
        }

        protected override async void DoReceive()
        {
            try
            {
                var statuses = await this._account.GetUserTimelineAsync(this._account.Id, count: 100);
                statuses.ForEach(StatusInbox.Queue);
            }
            catch (Exception ex)
            {
                BackstageModel.RegisterEvent(
                    new OperationFailedEvent("ツイートを受信できませんでした(@" + this._account.UnreliableScreenName + ")", ex));
            }
        }
    }
}