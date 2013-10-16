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
    public class HomeTimelineReceiver : CyclicReceiverBase
    {
        private readonly TwitterAccount _account;

        public HomeTimelineReceiver(TwitterAccount account)
        {
            this._account = account;
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
                    var recv = await this._account.GetHomeTimelineAsync(100);
                    recv.ForEach(StatusInbox.Queue);
                }
                catch (Exception ex)
                {
                    BackstageModel.RegisterEvent(
                        new OperationFailedEvent("タイムラインを受信できません(@" +
                                                 this._account.UnreliableScreenName + "): " +
                                                 ex.Message));
                }
            });
        }
    }
}
