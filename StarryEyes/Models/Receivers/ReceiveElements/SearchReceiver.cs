using System;
using System.Linq;
using StarryEyes.Breezy.Api.Rest;
using StarryEyes.Models.Backpanels.NotificationEvents;
using StarryEyes.Models.Stores;
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

        protected override void DoReceive()
        {
            var authInfo = AccountsStore.Accounts.Shuffle().Select(s => s.AuthenticateInfo).FirstOrDefault();
            if (authInfo == null)
            {
                BackpanelModel.RegisterEvent(new OperationFailedEvent("アカウントが登録されていないため、検索タイムラインを受信できませんでした。"));
                return;
            }
            authInfo.SearchTweets(_query)
                    .Subscribe(ReceiveInbox.Queue,
                               ex => BackpanelModel.RegisterEvent(
                                   new OperationFailedEvent("search receive error: \"" +
                                                            _query + "\", " + authInfo.UnreliableScreenName + " - " +
                                                            ex.Message)));
        }
    }
}
