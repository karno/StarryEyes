using System;
using Livet;
using StarryEyes.Models.Backpanels;
using StarryEyes.Models.Backpanels.SystemEvents;
using StarryEyes.Models.Backpanels.TwitterEvents;

namespace StarryEyes.Models
{
    /// <summary>
    /// バックパネル及びステータスエリアのモデル
    /// </summary>
    public static class BackpanelModel
    {
        static BackpanelModel()
        {
        }

        #region Account connection state management

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

        public static event Action<BackpanelEventBase> OnEventRegistered;

        public static void RegisterEvent(BackpanelEventBase ev)
        {
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

        public static void RemoveEvent(BackpanelEventBase ev)
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
