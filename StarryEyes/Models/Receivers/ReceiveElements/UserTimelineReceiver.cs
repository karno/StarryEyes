using System;
using System.Linq;
using StarryEyes.Anomaly.TwitterApi.Rest;
using StarryEyes.Models.Accounting;
using StarryEyes.Models.Backstages.NotificationEvents;
using StarryEyes.Settings;

namespace StarryEyes.Models.Receivers.ReceiveElements
{
    public class UserTimelineReceiver : CyclicReceiverBase
    {
        private readonly TwitterAccount _account;

        public UserTimelineReceiver(TwitterAccount account)
        {
            this._account = account;
        }

        protected override int IntervalSec
        {
            get { return Setting.RESTReceivePeriod.Value; }
        }

        protected override async void DoReceive()
        {
            try
            {
                var statuses = await this._account.GetUserTimeline(this._account.Id, count: 100);
                statuses.ForEach(ReceiveInbox.Queue);
            }
            catch (Exception ex)
            {
                BackstageModel.RegisterEvent(
                    new OperationFailedEvent("user status receive error: " +
                                             this._account.UnreliableScreenName + " - " + ex.Message));
            }
        }
    }
}