using System;
using System.Linq;
using Livet;
using StarryEyes.Models.Backpanels;
using StarryEyes.Models.Backpanels.SystemEvents;
using StarryEyes.Models.Backpanels.TwitterEvents;
using StarryEyes.Models.Hubs;

namespace StarryEyes.Models
{
    /// <summary>
    /// バックパネル及びステータスエリアのモデル
    /// </summary>
    public static class BackpanelModel
    {
        static BackpanelModel()
        {
            AppInformationHub.OnInformationPublished += AppInformationHub_OnInformationPublished;
        }

        #region Bind AppInformationHub

        private static readonly object _infoAddLockObject = new object();
        static void AppInformationHub_OnInformationPublished(AppInformation info)
        {
            lock (_infoAddLockObject)
            {
                _infoCollection.RemoveWhere(item => item.Id == info.Id);
                _infoCollection.Add(info);
            }
            RegisterEvent(new InternalErrorEvent(info.Header + ": " + info.Detail));
        }


        private static readonly ObservableSynchronizedCollectionEx<AppInformation> _infoCollection =
            new ObservableSynchronizedCollectionEx<AppInformation>();

        public static ObservableSynchronizedCollectionEx<AppInformation> InfoCollection
        {
            get { return _infoCollection; }
        }

        #endregion

        #region Account connection state management

        #endregion

        #region Event management

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
            System.Diagnostics.Debug.WriteLine(ev.Title + " " + ev.Detail);
            var handler = OnEventRegistered;
            if (handler != null)
                OnEventRegistered(ev);
            var te = ev as TwitterEventBase;
            if (te != null)
            {
                lock (_twitterEvents.SyncRoot)
                {
                    _twitterEvents.Insert(0, te);
                    if (_twitterEvents.Count > TwitterEventMaxHoldCount)
                        _twitterEvents.RemoveAt(_twitterEvents.Count - 1);
                }
            }
        }
        #endregion
    }
}
