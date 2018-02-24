using System;
using System.Collections.Generic;
using System.Linq;
using Cadena;
using Cadena.Api.Parameters;
using Cadena.Engine;
using Cadena.Engine.CyclicReceivers.Timelines;
using JetBrains.Annotations;
using StarryEyes.Globalization;
using StarryEyes.Globalization.Models;
using StarryEyes.Models.Accounting;
using StarryEyes.Models.Databases;
using StarryEyes.Models.Receiving.Ex;
using StarryEyes.Models.Receiving.Handling;
using StarryEyes.Nightmare.Windows;
using StarryEyes.Settings;

namespace StarryEyes.Models.Receiving.Managers
{
    internal class ListReceiveManager
    {
        private readonly IDictionary<ListInfo, ListParameter> _receiverDictionary =
            new Dictionary<ListInfo, ListParameter>();

        private readonly IDictionary<ListInfo, int> _listReceiverReferenceCount =
            new Dictionary<ListInfo, int>();

        private readonly IDictionary<long, ListReceiver> _listReceivers =
            new Dictionary<long, ListReceiver>();

        public void StartReceive(ListInfo info)
        {
            var account = Setting.Accounts.Collection.FirstOrDefault(
                a => a.UnreliableScreenName?.Equals(info.OwnerScreenName,
                         StringComparison.CurrentCultureIgnoreCase) ?? false);
            if (account != null)
            {
                StartReceive(info, account);
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
                    CommonButtons = TaskDialogCommonButtons.Close
                });
            }
        }

        public void StartReceive(ListInfo info, string receiverScreenName)
        {
            var account = Setting.Accounts.Collection.FirstOrDefault(
                a => a.UnreliableScreenName?.Equals(receiverScreenName,
                         StringComparison.CurrentCultureIgnoreCase) ?? false);
            if (account != null)
            {
                StartReceiveCore(info, account);
            }
            else
            {
                StartReceiveCore(info);
            }
        }

        public void StartReceive(ListInfo info, TwitterAccount account)
        {
            StartReceiveCore(info, account);
        }

        public void StartReceiveCore([NotNull] ListInfo info, TwitterAccount account = null)
        {
            lock (_listReceiverReferenceCount)
            {
                if (_listReceiverReferenceCount.ContainsKey(info))
                {
                    _listReceiverReferenceCount[info]++;
                }
                else
                {
                    _listReceiverReferenceCount.Add(info, 1);
                    var lparam = info.ToParameter();
                    _receiverDictionary.Add(info, lparam);
                    lock (_listReceivers)
                    {
                        var accessor = (IApiAccessor)account?.CreateAccessor() ?? new RandomAccountAccessor();
                        long id = account?.Id ?? 0;
                        if (!_listReceivers.TryGetValue(id, out var receiver))
                        {
                            receiver = new ListReceiver(accessor, StatusInbox.Enqueue, BackstageModel.NotifyException);
                            _listReceivers.Add(id, receiver);
                            ReceiveManager.ReceiveEngine.RegisterReceiver(receiver);
                        }
                        receiver.AddList(lparam);
                    }
                }
            }
        }

        public void StopReceive(ListInfo info)
        {
            lock (_listReceiverReferenceCount)
            {
                if (!_listReceiverReferenceCount.ContainsKey(info))
                {
                    return;
                }

                if (--_listReceiverReferenceCount[info] != 0) return;
                _listReceiverReferenceCount.Remove(info);
                // dispose receivers
                lock (_listReceivers)
                {
                    var lparam = _receiverDictionary[info];
                    _receiverDictionary.Remove(info);
                    foreach (var receiver in _listReceivers.Values)
                    {
                        receiver.RemoveList(lparam);
                    }
                }
            }
        }
    }

    internal class ListMemberReceiveManager
    {
        public event Action<ListInfo> ListMemberChanged;

        private readonly object _listReceiverLocker = new object();

        private readonly IDictionary<ListInfo, ListMemberListener> _receiverDictionary =
            new Dictionary<ListInfo, ListMemberListener>();

        private readonly IDictionary<ListInfo, int> _listReceiverReferenceCount =
            new Dictionary<ListInfo, int>();

        public void StartReceive(ListInfo info)
        {
            var account =
                Setting.Accounts.Collection.FirstOrDefault(
                    a => a.UnreliableScreenName?.Equals(info.OwnerScreenName,
                             StringComparison.CurrentCultureIgnoreCase) ?? false);
            if (account != null)
            {
                StartReceive(account, info);
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
                    CommonButtons = TaskDialogCommonButtons.Close
                });
            }
        }

        public void StartReceive(string receiverScreenName, ListInfo info)
        {
            var account =
                Setting.Accounts.Collection.FirstOrDefault(
                    a => a.UnreliableScreenName?.Equals(receiverScreenName,
                             StringComparison.CurrentCultureIgnoreCase) ?? false);
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
                    _listReceiverReferenceCount.Add(info, 1);
                    var lparam = info.ToParameter();
                    var listener = new ListMemberListener(account.CreateAccessor(), lparam);
                    _receiverDictionary.Add(info, listener);
                    listener.ListMemberChanged += (o, e) => ListMemberChanged?.Invoke(info);
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

                if (--_listReceiverReferenceCount[info] != 0) return;
                // dispose receivers
                _listReceiverReferenceCount.Remove(info);
                var d = _receiverDictionary[info];
                _receiverDictionary.Remove(info);
                d.Dispose();
            }
        }

        private sealed class ListMemberListener : IDisposable
        {
            private readonly ListParameter _listParam;
            private readonly ListMemberReceiver _receiver;

            private readonly HashSet<long> _members = new HashSet<long>();

            public event EventHandler<ListParameter> ListMemberChanged;

            public ListMemberListener(IApiAccessor accessor, ListParameter listParam)
            {
                _listParam = listParam;
                _receiver = new ListMemberReceiver(accessor, listParam, UserProxy.StoreUsers, UsersChanged,
                    BackstageModel.NotifyException);
                ReceiveManager.ReceiveEngine.RegisterReceiver(_receiver, RequestPriority.Low);
            }

            private void UsersChanged(IEnumerable<long> userIds)
            {
                var newset = new HashSet<long>(userIds);
                bool changed = false;
                foreach (var id in newset)
                {
                    if (_members.Add(id))
                        changed = true;
                }
                foreach (var id in _members.ToArray())
                {
                    if (!newset.Remove(id))
                    {
                        _members.Remove(id);
                        changed = true;
                    }
                }
                if (changed)
                {
                    ListMemberChanged?.Invoke(this, _listParam);
                }
            }

            public void Dispose()
            {
                ReceiveManager.ReceiveEngine.UnregisterReceiver(_receiver);
            }
        }
    }
}