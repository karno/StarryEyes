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
        #region Account connection state management

        #endregion

        #region Event management

        public const int TwitterEventMaxHoldCount = 256;

        private static readonly ObservableSynchronizedCollection<TwitterEventBase> _twitterEvents =
            new ObservableSynchronizedCollection<TwitterEventBase>();
        public static ObservableSynchronizedCollection<TwitterEventBase> TwitterEvents
        {
            get { return BackpanelModel._twitterEvents; }
        }

        public static event Action<BackpanelEventBase> OnEventRegistered;
        public static void RegisterEvent(BackpanelEventBase ev)
        {
            var handler = OnEventRegistered;
            if (handler != null)
                OnEventRegistered(ev);
            var te = ev as TwitterEventBase;
            if (te != null)
            {
                _twitterEvents.Insert(0, te);
                if (_twitterEvents.Count > TwitterEventMaxHoldCount)
                    _twitterEvents.RemoveAt(_twitterEvents.Count - 1);
            }
        }

        #endregion
    }
}
