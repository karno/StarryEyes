using System.Linq;
using System.Threading.Tasks;
using StarryEyes.Anomaly.TwitterApi.Rest;
using StarryEyes.Globalization;
using StarryEyes.Globalization.Models;
using StarryEyes.Models.Accounting;
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
            get { return ReceivingResources.ReceiverMentionFormat.SafeFormat("@" + _account.UnreliableScreenName); }
        }

        protected override int IntervalSec
        {
            get { return Setting.RESTReceivePeriod.Value; }
        }

        protected override async Task DoReceive()
        {
            (await this._account.GetMentionsAsync(100))
                .ForEach(StatusInbox.Enqueue);
        }
    }
}
