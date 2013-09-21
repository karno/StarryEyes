using System;
using System.Collections.Generic;
using System.Linq;
using StarryEyes.Models.Accounting;
using StarryEyes.Models.Receivers.ReceiveElements;
using StarryEyes.Nightmare.Windows;
using StarryEyes.Settings;

namespace StarryEyes.Models.Receivers.Managers
{
    internal class ListReceiveManager
    {
        private readonly object _listReceiverLocker = new object();
        private readonly IDictionary<ListInfo, ListReceiver> _listReceiverResolver
            = new Dictionary<ListInfo, ListReceiver>();
        private readonly IDictionary<ListInfo, int> _listReceiverReferenceCount
            = new Dictionary<ListInfo, int>();

        public void StartReceive(ListInfo info)
        {
            var account =
                            Setting.Accounts.Collection.FirstOrDefault(
                                a => a.UnreliableScreenName.Equals(info.OwnerScreenName, StringComparison.CurrentCultureIgnoreCase));
            if (account != null)
            {
                StartReceive(account, info);
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
            var account =
                Setting.Accounts.Collection.FirstOrDefault(
                    a => a.UnreliableScreenName.Equals(receiverScreenName, StringComparison.CurrentCultureIgnoreCase));
            if (account != null)
            {
                StartReceive(account, info);
            }
            else
            {
                StartReceive(info);
            }
        }

        public void StartReceive(TwitterAccount account, ListInfo info)
        {
            lock (_listReceiverLocker)
            {
                if (_listReceiverReferenceCount.ContainsKey(info))
                {
                    _listReceiverReferenceCount[info]++;
                }
                else
                {
                    var lr = new ListReceiver(account, info);
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
