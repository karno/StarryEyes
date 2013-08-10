using System;
using System.Linq;
using StarryEyes.Anomaly.TwitterApi.Rest;
using StarryEyes.Models.Accounting;
using StarryEyes.Models.Backstages.NotificationEvents;
using StarryEyes.Settings;

namespace StarryEyes.Models.Receivers.ReceiveElements
{
    public class MentionTimelineReceiver : CyclicReceiverBase
    {
        private readonly TwitterAccount _account;

        public MentionTimelineReceiver(TwitterAccount account)
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
                var mentions = await this._account.GetMentions(count: 100);
                mentions.ForEach(ReceiveInbox.Queue);
            }
            catch (Exception ex)
            {
                BackstageModel.RegisterEvent(
                    new OperationFailedEvent("返信を取得できません(@" + _account.UnreliableScreenName + "): " + ex.Message));
            }
        }
    }
}
