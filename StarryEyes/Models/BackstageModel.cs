using System;
using Livet;
using StarryEyes.Models.Backstages;
using StarryEyes.Models.Backstages.SystemEvents;
using StarryEyes.Models.Backstages.TwitterEvents;

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
