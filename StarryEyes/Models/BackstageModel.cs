using System;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive.Linq;
using Livet;
using StarryEyes.Breezy.Authorize;
using StarryEyes.Models.Backstages;
using StarryEyes.Models.Backstages.SystemEvents;
using StarryEyes.Models.Backstages.TwitterEvents;
using StarryEyes.Models.Receivers;
using StarryEyes.Models.Stores;
using StarryEyes.Settings;

namespace StarryEyes.Models
{
    /// <summary>
    /// バックパネル及びステータスエリアのモデル
    /// </summary>
    public static class BackstageModel
    {
        static BackstageModel()
        {
        }

        internal static void Initialize()
        {
            AccountsStore.Accounts
                        .ListenCollectionChanged()
                        .Subscribe(e =>
                        {
                            var newItem = e.NewItems.Count > 0
                                              ? new BackstageAccountModel(
                                                    ((AccountSetting)e.NewItems[0]).AuthenticateInfo)
                                              : null;
                            switch (e.Action)
                            {
                                case NotifyCollectionChangedAction.Add:
                                    _accounts.Insert(e.NewStartingIndex, newItem);
                                    break;
                                case NotifyCollectionChangedAction.Remove:
                                    _accounts.RemoveAt(e.OldStartingIndex);
                                    break;
                                case NotifyCollectionChangedAction.Replace:
                                    _accounts[e.NewStartingIndex] = newItem;
                                    break;
                                case NotifyCollectionChangedAction.Move:
                                    _accounts.Move(e.OldStartingIndex, e.NewStartingIndex);
                                    break;
                                case NotifyCollectionChangedAction.Reset:
                                    _accounts.Clear();
                                    AccountsStore.Accounts.ForEach(
                                        i => _accounts.Add(new BackstageAccountModel(i.AuthenticateInfo)));
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                        });
            AccountsStore.Accounts.ForEach(
                i => _accounts.Add(new BackstageAccountModel(i.AuthenticateInfo)));
            ReceiversManager.UserStreamsConnectionStateChanged += UpdateConnectionState;
        }

        static void UpdateConnectionState(long id)
        {
            var avm = _accounts.FirstOrDefault(vm => vm.Info.Id == id);
            if (avm != null)
            {
                avm.UpdateConnectionState();
            }
        }

        #region Account connection state management

        private static readonly ObservableSynchronizedCollectionEx<BackstageAccountModel> _accounts =
            new ObservableSynchronizedCollectionEx<BackstageAccountModel>();
        public static ObservableSynchronizedCollectionEx<BackstageAccountModel> Accounts
        {
            get { return _accounts; }
        }

        #endregion

        #region Event management

        private static readonly ObservableSynchronizedCollectionEx<SystemEventBase> _systemEvents =
            new ObservableSynchronizedCollectionEx<SystemEventBase>();
        public static ObservableSynchronizedCollectionEx<SystemEventBase> SystemEvents
        {
            get { return _systemEvents; }
        }

        public const int TwitterEventMaxHoldCount = 256;

        private static readonly ObservableSynchronizedCollection<TwitterEventBase> _twitterEvents =
            new ObservableSynchronizedCollection<TwitterEventBase>();
        public static ObservableSynchronizedCollection<TwitterEventBase> TwitterEvents
        {
            get { return _twitterEvents; }
        }

        public static event Action<BackstageEventBase> OnEventRegistered;

        public static void RegisterEvent(BackstageEventBase ev)
        {
            System.Diagnostics.Debug.WriteLine("EVENT: " + ev.Title + " - " + ev.Detail);
            var handler = OnEventRegistered;
            if (handler != null)
                OnEventRegistered(ev);
            var tev = ev as TwitterEventBase;
            if (tev != null)
            {
                lock (_twitterEvents.SyncRoot)
                {
                    _twitterEvents.Insert(0, tev);
                    if (_twitterEvents.Count > TwitterEventMaxHoldCount)
                        _twitterEvents.RemoveAt(_twitterEvents.Count - 1);
                }
                return;
            }
            var sev = ev as SystemEventBase;
            if (sev != null)
            {
                lock (SystemEvents.SyncRoot)
                {
                    if (!String.IsNullOrEmpty(sev.Id))
                    {
                        SystemEvents.RemoveWhere(e => e.Id == sev.Id);
                    }
                    SystemEvents.Add(sev);
                }
                return;
            }
        }

        public static void RemoveEvent(BackstageEventBase ev)
        {
            var tev = ev as TwitterEventBase;
            if (tev != null)
            {
                lock (_twitterEvents.SyncRoot)
                {
                    _twitterEvents.Remove(tev);
                }
                return;
            }
            var sev = ev as SystemEventBase;
            if (sev != null)
            {
                lock (SystemEvents.SyncRoot)
                {
                    SystemEvents.Remove(sev);
                }
                return;
            }
        }

        public static void RemoveEvent(string id)
        {
            lock (SystemEvents.SyncRoot)
            {
                SystemEvents.RemoveWhere(e => e.Id == id);
            }
        }

        #endregion

    }
}
