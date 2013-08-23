using System;
using System.Linq;
using System.Threading.Tasks;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.TwitterApi.Rest;
using StarryEyes.Anomaly.Utils;
using StarryEyes.Models.Accounting;
using StarryEyes.Models.Backstages.NotificationEvents;
using StarryEyes.Settings;

namespace StarryEyes.Models.Receivers.ReceiveElements
{
    public class ListReceiver : CyclicReceiverBase
    {
        private readonly ListInfo _listInfo;
        private readonly TwitterAccount _auth;

        public ListReceiver(ListInfo listInfo)
        {
            _listInfo = listInfo;
        }

        public ListReceiver(TwitterAccount auth, ListInfo listInfo)
        {
            _auth = auth;
            _listInfo = listInfo;
        }

        protected override int IntervalSec
        {
            get { return Setting.ListReceivePeriod.Value; }
        }

        protected override void DoReceive()
        {
            Task.Run(async () =>
            {
                var authInfo = this._auth ?? Setting.Accounts.GetRandomOne();
                if (authInfo == null)
                {
                    BackstageModel.RegisterEvent(new OperationFailedEvent("アカウントが登録されていないため、検索タイムラインを受信できませんでした。"));
                    return;
                }

                try
                {
                    var statuses =
                        await authInfo.GetListTimelineAsync(_listInfo.Slug, _listInfo.OwnerScreenName);
                    statuses.ForEach(ReceiveInbox.Queue);
                }
                catch (Exception ex)
                {
                    BackstageModel.RegisterEvent(
                        new OperationFailedEvent("list receive error: \"" +
                                                 this._listInfo + "\", " +
                                                 authInfo.UnreliableScreenName + " - " +
                                                 ex.Message));
                }
            });
        }

        public static IObservable<TwitterStatus> DoReceive(TwitterAccount info, ListInfo list, long? maxId = null)
        {
            return info.GetListTimelineAsync(list.Slug, list.OwnerScreenName, maxId: maxId).ToObservable();
        }

    }
}
