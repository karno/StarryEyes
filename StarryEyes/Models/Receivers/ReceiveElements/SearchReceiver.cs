using System;
using System.Linq;
using StarryEyes.Anomaly.TwitterApi.Rest;
using StarryEyes.Models.Backstages.NotificationEvents;
using StarryEyes.Settings;

namespace StarryEyes.Models.Receivers.ReceiveElements
{
    public class SearchReceiver : CyclicReceiverBase
    {
        private readonly string _query;

        public SearchReceiver(string query)
        {
            _query = query;
        }

        protected override int IntervalSec
        {
            get { return Setting.RESTSearchReceivePeriod.Value; }
        }

        protected override async void DoReceive()
        {
            try
            {
                var account = Setting.Accounts.GetRandomOne();
                if (account == null)
                {
                    BackstageModel.RegisterEvent(new OperationFailedEvent("アカウントが登録されていないため、検索タイムラインを受信できませんでした。"));
                    return;
                }
                var resp = await account.Search(_query);
                resp.ForEach(ReceiveInbox.Queue);
            }
            catch (Exception ex)
            {
                BackstageModel.RegisterEvent(new OperationFailedEvent("検索タイムラインを受信できません: " + ex.Message));
            }
        }
    }
}
