using System;
using System.Linq;
using Livet;
using StarryEyes.Albireo.Helpers;
using StarryEyes.Models.Accounting;
using StarryEyes.Models.Backstages;
using StarryEyes.Models.Backstages.NotificationEvents;
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

        static void UpdateConnectionState(long id)
        {
            var avm = _accounts.FirstOrDefault(vm => vm.Account.Id == id);
            avm?.UpdateConnectionState();
        }

        #region Account and connection state management

        private static readonly ObservableSynchronizedCollectionEx<BackstageAccountModel> _accounts =
            new ObservableSynchronizedCollectionEx<BackstageAccountModel>();

        public static ObservableSynchronizedCollectionEx<BackstageAccountModel> Accounts => _accounts;

        public static void NotifyFallbackState(TwitterAccount account, bool isFallbacked)
        {
            var model = _accounts.FirstOrDefault(a => a.Account.Id == account.Id);
            model?.NotifyFallbackState(isFallbacked);
        }

        #endregion Account and connection state management

        #region Event management

        public const int TwitterEventMaxHoldCount = 256;

        private static readonly ObservableSynchronizedCollectionEx<TwitterEventBase> _twitterEvents =
            new ObservableSynchronizedCollectionEx<TwitterEventBase>();

        public static ObservableSynchronizedCollectionEx<TwitterEventBase> TwitterEvents => _twitterEvents;

        public static event Action<BackstageEventBase> EventRegistered;

        public static void RegisterEvent(BackstageEventBase ev)
        {
            System.Diagnostics.Debug.WriteLine("EVENT: " + ev.Title + " - " + ev.Detail);
            EventRegistered?.Invoke(ev);
            var tev = ev as TwitterEventBase;
            if (tev == null) return;
            lock (_twitterEvents.SyncRoot)
            {
                if (tev.IsLocalUserInvolved)
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

        #endregion Event management

        public static event Action CloseBackstage;

        public static void RaiseCloseBackstage()
        {
            CloseBackstage?.Invoke();
        }

        public static void NotifyException(Exception ex)
        {
            RegisterEvent(new OperationFailedEvent("FAIL", ex));
        }
    }
}