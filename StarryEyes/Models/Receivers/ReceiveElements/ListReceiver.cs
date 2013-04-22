using System;
using System.Linq;
using StarryEyes.Breezy.Api.Rest;
using StarryEyes.Breezy.Authorize;
using StarryEyes.Breezy.DataModel;
using StarryEyes.Models.Backpanels.NotificationEvents;
using StarryEyes.Models.Stores;
using StarryEyes.Settings;

namespace StarryEyes.Models.Receivers.ReceiveElements
{
    public class ListReceiver : CyclicReceiverBase
    {
        private readonly ListInfo _listInfo;
        private readonly AuthenticateInfo _auth;

        public ListReceiver(ListInfo listInfo)
        {
            _listInfo = listInfo;
        }

        public ListReceiver(AuthenticateInfo auth, ListInfo listInfo)
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
            var authInfo = _auth;
            if (authInfo == null)
            {
                authInfo = AccountsStore.Accounts.Shuffle().Select(s => s.AuthenticateInfo).FirstOrDefault();

            }
            if (authInfo == null)
            {
                BackpanelModel.RegisterEvent(new OperationFailedEvent("アカウントが登録されていないため、検索タイムラインを受信できませんでした。"));
                return;
            }
            authInfo.GetListStatuses(slug: _listInfo.Slug, owner_screen_name: _listInfo.OwnerScreenName)
                    .Subscribe(ReceiveInbox.Queue,
                               ex => BackpanelModel.RegisterEvent(
                                   new OperationFailedEvent("search receive error: \"" +
                                                            _listInfo.ToString() + "\", " +
                                                            authInfo.UnreliableScreenName + " - " +
                                                            ex.Message)));
        }

        public static IObservable<TwitterStatus> DoReceive(AuthenticateInfo info, ListInfo list, long? maxId = null)
        {
            return info.GetListStatuses(slug: list.Slug, owner_screen_name: list.OwnerScreenName, max_id: maxId);
        }

    }
}
