using System.Collections.Generic;
using System.Linq;
using StarryEyes.Breezy.Authorize;
using StarryEyes.Models.Backpanels.SystemEvents;
using StarryEyes.Models.Receivers.ReceiveElements;
using StarryEyes.Models.Stores;

namespace StarryEyes.Models.Receivers.Managers
{
    internal class ListReceiveManager
    {
        private readonly object _listReceiverLocker = new object();
        private readonly SortedDictionary<ListInfo, ListReceiver> _listReceiverResolver
            = new SortedDictionary<ListInfo, ListReceiver>();
        private readonly SortedDictionary<ListInfo, int> _listReceiverReferenceCount
            = new SortedDictionary<ListInfo, int>();

        public void StartReceive(ListInfo info)
        {
            var ai = AccountsStore.Accounts.FirstOrDefault(aset => aset.AuthenticateInfo.UnreliableScreenName == info.OwnerScreenName);
            if (ai != null)
            {
                StartReceive(ai.AuthenticateInfo, info);
            }
            else
            {
                BackpanelModel.RegisterEvent(new ListReceiveFailedEvent(info));
            }
        }

        public void StartReceive(string receiverScreenName, ListInfo info)
        {
            var ai = AccountsStore.Accounts.FirstOrDefault(aset => aset.AuthenticateInfo.UnreliableScreenName == receiverScreenName);
            if (ai != null)
                StartReceive(ai.AuthenticateInfo, info);
            else
                StartReceive(info);
        }

        public void StartReceive(AuthenticateInfo auth, ListInfo info)
        {
            lock (_listReceiverLocker)
            {
                if (_listReceiverReferenceCount.ContainsKey(info))
                {
                    _listReceiverReferenceCount[info]++;
                }
                else
                {
                    var lr = new ListReceiver(auth, info);
                    _listReceiverReferenceCount.Add(info, 1);
                    _listReceiverResolver.Add(info, lr);
                }
            }
        }

        public void StopReceive(ListInfo info)
        {
            lock (_listReceiverLocker)
            {
                if (!_listReceiverReferenceCount.ContainsKey(info))
                {
                    return;
                }

                if (--_listReceiverReferenceCount[info] == 0)
                {
                    // dispose connection
                    _listReceiverReferenceCount.Remove(info);
                    var lr = _listReceiverResolver[info];
                    _listReceiverResolver.Remove(info);
                    lr.Dispose();
                }
            }
        }
    }
}
