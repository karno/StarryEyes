using System;
using System.Collections.Generic;
using System.Linq;
using StarryEyes.Albireo;
using StarryEyes.Albireo.Helpers;
using StarryEyes.Globalization;
using StarryEyes.Globalization.Models;
using StarryEyes.Models.Accounting;
using StarryEyes.Models.Receiving.Receivers;
using StarryEyes.Nightmare.Windows;
using StarryEyes.Settings;

namespace StarryEyes.Models.Receiving.Managers
{
    internal class ListReceiveManager
    {
        private readonly object _listReceiverLocker = new object();

        private readonly IDictionary<ListInfo, IDisposable> _receiverDictionary =
            new Dictionary<ListInfo, IDisposable>();

        private readonly IDictionary<ListInfo, int> _listReceiverReferenceCount =
            new Dictionary<ListInfo, int>();

        public void StartReceive(ListInfo info)
        {
            var account =
                Setting.Accounts.Collection.FirstOrDefault(
                    a => a.UnreliableScreenName.Equals(info.OwnerScreenName, StringComparison.CurrentCultureIgnoreCase));
            if (account != null)
            {
                this.StartReceive(account, info);
            }
            else
            {
                MainWindowModel.ShowTaskDialog(new TaskDialogOptions
                {
                    Title = ReceivingResources.MsgListReceiveErrorTitle,
                    MainIcon = VistaTaskDialogIcon.Error,
                    MainInstruction = ReceivingResources.MsgListReceiveErrorInst,
                    Content = ReceivingResources.MsgListReceiveErrorContentFormat.SafeFormat(info),
                    ExpandedInfo = ReceivingResources.MsgListReceiveErrorExInfo,
                    ExpandedByDefault = true,
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
                this.StartReceive(account, info);
            }
            else
            {
                this.StartReceive(info);
            }
        }

        public void StartReceive(TwitterAccount account, ListInfo info)
        {
            lock (this._listReceiverLocker)
            {
                if (this._listReceiverReferenceCount.ContainsKey(info))
                {
                    this._listReceiverReferenceCount[info]++;
                }
                else
                {
                    this._listReceiverReferenceCount.Add(info, 1);
                    this._receiverDictionary.Add(info, new ListReceiver(account, info));
                }
            }
        }

        public void StopReceive(ListInfo info)
        {
            lock (this._listReceiverLocker)
            {
                if (!this._listReceiverReferenceCount.ContainsKey(info))
                {
                    return;
                }

                if (--this._listReceiverReferenceCount[info] != 0) return;
                // dispose receivers
                this._listReceiverReferenceCount.Remove(info);
                var d = _receiverDictionary[info];
                _receiverDictionary.Remove(info);
                d.Dispose();
            }
        }
    }

    internal class ListMemberReceiveManager
    {
        public event Action<ListInfo> ListMemberChanged;

        private readonly object _listReceiverLocker = new object();

        private readonly IDictionary<ListInfo, IDisposable> _receiverDictionary =
            new Dictionary<ListInfo, IDisposable>();

        private readonly IDictionary<ListInfo, int> _listReceiverReferenceCount =
            new Dictionary<ListInfo, int>();

        public void StartReceive(ListInfo info)
        {
            var account =
                Setting.Accounts.Collection.FirstOrDefault(
                    a => a.UnreliableScreenName.Equals(info.OwnerScreenName, StringComparison.CurrentCultureIgnoreCase));
            if (account != null)
            {
                this.StartReceive(account, info);
            }
            else
            {
                MainWindowModel.ShowTaskDialog(new TaskDialogOptions
                {
                    Title = ReceivingResources.MsgListReceiveErrorTitle,
                    MainIcon = VistaTaskDialogIcon.Error,
                    MainInstruction = ReceivingResources.MsgListReceiveErrorInst,
                    Content = ReceivingResources.MsgListReceiveErrorContentFormat.SafeFormat(info),
                    ExpandedInfo = ReceivingResources.MsgListReceiveErrorExInfo,
                    ExpandedByDefault = true,
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
                this.StartReceive(account, info);
            }
            else
            {
                this.StartReceive(info);
            }
        }

        public void StartReceive(TwitterAccount account, ListInfo info)
        {
            lock (this._listReceiverLocker)
            {
                if (this._listReceiverReferenceCount.ContainsKey(info))
                {
                    this._listReceiverReferenceCount[info]++;
                }
                else
                {
                    var lmr = new ListMemberReceiver(account, info);
                    lmr.ListMemberChanged += () => ListMemberChanged.SafeInvoke(info);
                    this._listReceiverReferenceCount.Add(info, 1);
                    this._receiverDictionary.Add(info, lmr);
                }
            }
        }

        public void StopReceive(ListInfo info)
        {
            lock (this._listReceiverLocker)
            {
                if (!this._listReceiverReferenceCount.ContainsKey(info))
                {
                    return;
                }

                if (--this._listReceiverReferenceCount[info] != 0) return;
                // dispose receivers
                this._listReceiverReferenceCount.Remove(info);
                var d = _receiverDictionary[info];
                _receiverDictionary.Remove(info);
                d.Dispose();
            }
        }
    }
}
