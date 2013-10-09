using System;
using StarryEyes.Anomaly.TwitterApi.Rest;
using StarryEyes.Anomaly.Utils;
using StarryEyes.Models.Accounting;
using StarryEyes.Models.Backstages.NotificationEvents;
using StarryEyes.Models.Receiving.Handling;
using StarryEyes.Settings;

namespace StarryEyes.Models.Receiving.Receivers
{
    public class DirectMessagesReceiver : CyclicReceiverBase
    {
        private readonly TwitterAccount _account;

        public DirectMessagesReceiver(TwitterAccount account)
        {
            this._account = account;
        }

        protected override int IntervalSec
        {
            get { return Setting.RESTReceivePeriod.Value; }
        }

        protected override void DoReceive()
        {
            this._account.GetDirectMessagesAsync(count: 50).ToObservable()
                .Subscribe(StatusInbox.Queue,
                           ex => BackstageModel.RegisterEvent(
                               new OperationFailedEvent("messages receive error: " +
                                                        this._account.UnreliableScreenName + " - " +
                                                        ex.Message)));
            this._account.GetSentDirectMessagesAsync(count: 50).ToObservable()
                .Subscribe(StatusInbox.Queue,
                           ex => BackstageModel.RegisterEvent(
                               new OperationFailedEvent("sent messages receive error: " +
                                                        this._account.UnreliableScreenName + " - " +
                                                        ex.Message)));
        }
    }
}
