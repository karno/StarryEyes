using System;
using System.Linq;
using System.Reactive.Linq;
using Livet;
using StarryEyes.Models.Backstages;
using StarryEyes.Models.Backstages.TwitterEvents;
using StarryEyes.Models.Receivers;
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
            Setting.Accounts.Collection.ListenCollectionChanged()
                   .Subscribe(_ =>
                   {
                       _accounts.Clear();
                       Setting.Accounts.Collection.ForEach(a => _accounts.Add(new BackstageAccountModel(a)));
                   });
            Setting.Accounts.Collection.ForEach(a => _accounts.Add(new BackstageAccountModel(a)));
            ReceiversManager.UserStreamsConnectionStateChanged += UpdateConnectionState;
        }

        static void UpdateConnectionState(long id)
        {
            var avm = _accounts.FirstOrDefault(vm => vm.Account.Id == id);
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

        public const int TwitterEventMaxHoldCount = 256;

        private static readonly ObservableSynchronizedCollection<TwitterEventBase> _twitterEvents =
            new ObservableSynchronizedCollection<TwitterEventBase>();
        public static ObservableSynchronizedCollection<TwitterEventBase> TwitterEvents
        {
            get { return _twitterEvents; }
        }

        public static event Action<BackstageEventBase> EventRegistered;

        public static void RegisterEvent(BackstageEventBase ev)
        {
            System.Diagnostics.Debug.WriteLine("EVENT: " + ev.Title + " - " + ev.Detail);
            var handler = EventRegistered;
            if (handler != null)
                EventRegistered(ev);
            var tev = ev as TwitterEventBase;
            if (tev == null) return;
            lock (_twitterEvents.SyncRoot)
            {
                _twitterEvents.Insert(0, tev);
                if (_twitterEvents.Count > TwitterEventMaxHoldCount)
                    _twitterEvents.RemoveAt(_twitterEvents.Count - 1);
            }
        }

        public static void RemoveEvent(BackstageEventBase ev)
        {
            var tev = ev as TwitterEventBase;
            if (tev == null) return;
            lock (_twitterEvents.SyncRoot)
            {
                _twitterEvents.Remove(tev);
            }
        }

        #endregion

        public static event Action CloseBackstage;
        public static void RaiseCloseBackstage()
        {
            var handler = CloseBackstage;
            if (handler != null) handler();
        }
    }
}
