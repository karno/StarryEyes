using System;
using System.Collections.Generic;
using System.Linq;
using StarryEyes.Breezy.Authorize;
using StarryEyes.Models.Receivers.ReceiveElements;
using StarryEyes.Models.Stores;
using StarryEyes.Nightmare.Windows;

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
                MainWindowModel.ShowTaskDialog(new TaskDialogOptions
                {
                    MainIcon = VistaTaskDialogIcon.Error,
                    MainInstruction = "リスト受信を開始できません。",
                    Content = "リスト " + info + " を受信するアカウントを特定できませんでした。",
                    ExpandedInfo = "自分以外が作成したリストを受信する際は、そのリストをどのアカウントで受信するかを明示的に記述しなければなりません。" + Environment.NewLine +
                                   "例: receiver/user/listname",
                    ExpandedByDefault = true,
                    Title = "リスト受信エラー",
                    CommonButtons = TaskDialogCommonButtons.Close,
                });
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

                if (--this._listReceiverReferenceCount[info] != 0) return;
                // dispose connection
                this._listReceiverReferenceCount.Remove(info);
                var lr = this._listReceiverResolver[info];
                this._listReceiverResolver.Remove(info);
                lr.Dispose();
            }
        }
    }
}
