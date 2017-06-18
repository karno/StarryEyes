using System;
using System.Linq;
using Livet;
using StarryEyes.Albireo.Helpers;
using StarryEyes.Models.Accounting;
using StarryEyes.Models.Backstages;
using StarryEyes.Models.Backstages.TwitterEvents;
using StarryEyes.Models.Receiving;
using StarryEyes.Settings;

namespace StarryEyes.Models
{
    /// <summary>
    /// Model for backstage and status area.
    /// </summary>
    public static class BackstageModel
    {
        static BackstageModel()
        {
        }

        internal static void Initialize()
        {
            Setting.Accounts.Collection.ListenCollectionChanged(_ =>
            {
                _accounts.Clear();
                Setting.Accounts.Collection.ForEach(a => _accounts.Add(new BackstageAccountModel(a)));
            });
            Setting.Accounts.Collection.ForEach(a => _accounts.Add(new BackstageAccountModel(a)));
            ReceiveManager.UserStreamsConnectionStateChanged += UpdateConnectionState;
        }

        static void UpdateConnectionState(TwitterAccount account)
        {
            var avm = _accounts.FirstOrDefault(vm => vm.Account.Id == account.Id);
            if (avm != null)
            {
                avm.UpdateConnectionState();
            }
        }

        #region Account and connection state management

        private static readonly ObservableSynchronizedCollectionEx<BackstageAccountModel> _accounts =
            new ObservableSynchronizedCollectionEx<BackstageAccountModel>();
        public static ObservableSynchronizedCollectionEx<BackstageAccountModel> Accounts
        {
            get { return _accounts; }
        }

        public static void NotifyFallbackState(TwitterAccount account, bool isFallbacked)
        {
            var model = _accounts.FirstOrDefault(a => a.Account.Id == account.Id);
            if (model != null)
            {
                model.NotifyFallbackState(isFallbacked);
            }
        }

        #endregion

        #region Event management

        public const int TwitterEventMaxHoldCount = 256;

        private static readonly ObservableSynchronizedCollectionEx<TwitterEventBase> _twitterEvents =
            new ObservableSynchronizedCollectionEx<TwitterEventBase>();
        public static ObservableSynchronizedCollectionEx<TwitterEventBase> TwitterEvents
        {
            get { return _twitterEvents; }
        }

        public static event Action<BackstageEventBase> EventRegistered;

        public static void RegisterEvent(BackstageEventBase ev)
        {
            System.Diagnostics.Debug.WriteLine("EVENT: " + ev.Title + " - " + ev.Detail);
            EventRegistered.SafeInvoke(ev);
            var tev = ev as TwitterEventBase;
            if (tev == null) return;
            lock (_twitterEvents.SyncRoot)
            {
                if(tev.IsLocalUserInvolved)
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
            CloseBackstage.SafeInvoke();
        }
    }
}
