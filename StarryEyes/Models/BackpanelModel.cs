using System;
using Livet;
using StarryEyes.Models.Backpanels;
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

        private static readonly ObservableSynchronizedCollectionEx<BackpanelEventBase> _events =
            new ObservableSynchronizedCollectionEx<BackpanelEventBase>();

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
            switch (ev.RegisterKind)
            {
                case EventRegistingKind.IdExclusive:
                    lock (_events.SyncRoot)
                    {
                        _events.RemoveWhere(e => e.Id == ev.Id);
                        _events.Add(ev);
                    }
                    break;
                case EventRegistingKind.TwitterQueue:
                    var tev = ev as TwitterEventBase;
                    if (tev == null)
                    {
                        System.Diagnostics.Debug.WriteLine("TwitterEvent must be inherited TwitterEventBase.");
                    }
                    lock (_twitterEvents.SyncRoot)
                    {
                        _twitterEvents.Insert(0, tev);
                        if (_twitterEvents.Count > TwitterEventMaxHoldCount)
                            _twitterEvents.RemoveAt(_twitterEvents.Count - 1);
                    }
                    break;
                case EventRegistingKind.Always:
                    lock (_events.SyncRoot)
                    {
                        _events.Add(ev);
                    }
                    break;
            }
        }

        #endregion
    }
}
