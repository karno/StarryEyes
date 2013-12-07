using System;
using System.Linq;
using System.Threading.Tasks;
using StarryEyes.Anomaly.TwitterApi.Rest;
using StarryEyes.Models.Accounting;
using StarryEyes.Models.Backstages.NotificationEvents;
using StarryEyes.Models.Receiving.Handling;
using StarryEyes.Settings;

namespace StarryEyes.Models.Receiving.Receivers
{
    public class MentionTimelineReceiver : CyclicReceiverBase
    {
        private readonly TwitterAccount _account;

        public MentionTimelineReceiver(TwitterAccount account)
        {
            this._account = account;
        }

        protected override string ReceiverName
        {
            get { return "返信(@" + _account.UnreliableScreenName + ")"; }
        }

        protected override int IntervalSec
        {
            get { return Setting.RESTReceivePeriod.Value; }
        }

        protected override void DoReceive()
        {
            Task.Run(async () =>
            {
                try
                {
                    var mentions = await this._account.GetMentionsAsync(count: 100);
                    mentions.ForEach(StatusInbox.Queue);
                }
                catch (Exception ex)
                {
                    BackstageModel.RegisterEvent(new OperationFailedEvent(
                        "返信を取得できません(@" + this._account.UnreliableScreenName + ")", ex));
                }
            });
        }
    }
}
