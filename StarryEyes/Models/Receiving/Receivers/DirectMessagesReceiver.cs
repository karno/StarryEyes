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
    public class DirectMessagesReceiver : CyclicReceiverBase
    {
        private readonly TwitterAccount _account;

        public DirectMessagesReceiver(TwitterAccount account)
        {
            this._account = account;
        }

        protected override string ReceiverName
        {
            get
            {
                return ReceivingResources.ReceiverDirectMessageFormat.SafeFormat("@" + _account.UnreliableScreenName);
            }
        }

        protected override int IntervalSec
        {
            get { return Setting.RESTReceivePeriod.Value; }
        }

        protected override async Task DoReceive()
        {
            await Task.WhenAll(
                Task.Run(async () => (await this._account.GetDirectMessagesAsync(50))
                    .ForEach(StatusInbox.Enqueue)),
                Task.Run(async () => (await this._account.GetSentDirectMessagesAsync(50))
                    .ForEach(StatusInbox.Enqueue)));
        }
    }
}
