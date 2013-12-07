using System;
using System.Linq;
using System.Threading.Tasks;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.TwitterApi.Rest;
using StarryEyes.Anomaly.Utils;
using StarryEyes.Models.Accounting;
using StarryEyes.Models.Backstages.NotificationEvents;
using StarryEyes.Models.Receivers;
using StarryEyes.Models.Receiving.Handling;
using StarryEyes.Settings;

namespace StarryEyes.Models.Receiving.Receivers
{
    public class ListReceiver : CyclicReceiverBase
    {
        private readonly ListInfo _listInfo;
        private readonly TwitterAccount _auth;

        public ListReceiver(ListInfo listInfo)
        {
            this._listInfo = listInfo;
        }

        public ListReceiver(TwitterAccount auth, ListInfo listInfo)
        {
            this._auth = auth;
            this._listInfo = listInfo;
        }

        protected override string ReceiverName
        {
            get
            {
                return "リストタイムライン(" +
                    (_auth == null ? "アカウント未指定" : "@" + _auth.UnreliableScreenName) + " - " +
                       this._listInfo;
            }
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
                    BackstageModel.RegisterEvent(new OperationFailedEvent(
                        "アカウントが登録されていないため、検索タイムラインを受信できませんでした。", null));
                    return;
                }

                try
                {
                    var statuses =
                        await authInfo.GetListTimelineAsync(this._listInfo.Slug, this._listInfo.OwnerScreenName);
                    statuses.ForEach(StatusInbox.Queue);
                }
                catch (Exception ex)
                {
                    BackstageModel.RegisterEvent(new OperationFailedEvent(
                        "リストを受信できません: (@" + authInfo.UnreliableScreenName + " - \"" + this._listInfo + "\")", ex));
                }
            });
        }

        public static IObservable<TwitterStatus> DoReceive(TwitterAccount info, ListInfo list, long? maxId = null)
        {
            return info.GetListTimelineAsync(list.Slug, list.OwnerScreenName, maxId: maxId).ToObservable();
        }

    }
}
