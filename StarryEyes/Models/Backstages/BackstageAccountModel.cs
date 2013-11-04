using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Models.Accounting;
using StarryEyes.Models.Backstages.NotificationEvents;
using StarryEyes.Models.Backstages.NotificationEvents.PostEvents;
using StarryEyes.Models.Receiving;
using StarryEyes.Models.Receiving.Receivers;
using StarryEyes.Models.Stores;
using StarryEyes.Models.Subsystems;
using StarryEyes.Settings;

namespace StarryEyes.Models.Backstages
{
    public class BackstageAccountModel
    {
        private readonly TwitterAccount _account;
        private UserStreamsConnectionState _connectionState;
        private TwitterUser _user;

        public event Action ConnectionStateChanged;

        protected virtual void OnConnectionStateChanged()
        {
            var handler = this.ConnectionStateChanged;
            if (handler != null) handler();
        }

        public event Action TwitterUserChanged;

        protected virtual void OnTwitterUserChanged()
        {
            var handler = this.TwitterUserChanged;
            if (handler != null) handler();
        }

        public TwitterAccount Account
        {
            get { return this._account; }
        }

        public UserStreamsConnectionState ConnectionState
        {
            get { return this._connectionState; }
            private set
            {
                if (this._connectionState == value) return;
                this._connectionState = value;
                this.OnConnectionStateChanged();
            }
        }

        public TwitterUser User
        {
            get { return this._user; }
        }

        public int CurrentPostCount
        {
            get { return PostLimitPredictionService.GetCurrentWindowCount(this.Account.Id); }
        }

        public BackstageAccountModel(TwitterAccount account)
        {
            this._account = account;
            this.UpdateConnectionState();
            StoreHelper.GetUser(this._account.Id)
                       .Subscribe(
                           u =>
                           {
                               _user = u;
                               this.OnTwitterUserChanged();
                           },
                           ex => BackstageModel.RegisterEvent(
                               new OperationFailedEvent("Could not receive user account: " +
                                                        this._account.UnreliableScreenName +
                                                        " - " + ex.Message)));
        }

        internal void UpdateConnectionState()
        {
            this.ConnectionState = ReceiveManager.GetConnectionState(this.Account.Id);
        }

        public void Reconnect()
        {
            ReceiveManager.ReconnectUserStreams(this.Account.Id);
        }

        public event Action FallbackStateUpdated;

        protected virtual void RaiseFallbackStateUpdated()
        {
            var handler = this.FallbackStateUpdated;
            if (handler != null) handler();
        }

        public bool IsFallbacked { get; private set; }

        public DateTime FallbackPredictedReleaseTime { get; private set; }

        private IDisposable _prevScheduled;

        public void NotifyFallbackState(bool isFallbacked)
        {
            if (!isFallbacked && !this.IsFallbacked) return;
            Task.Run(() =>
            {
                this.IsFallbacked = isFallbacked;
                if (isFallbacked)
                {
                    // calc prediction
                    var threshold = DateTime.Now - TimeSpan.FromSeconds(Setting.PostWindowTimeSec.Value);
                    var oldest = PostLimitPredictionService.GetStatuses(Account.Id)
                                                           .Where(t => t.CreatedAt > threshold)
                                                           .OrderByDescending(t => t.CreatedAt)
                                                           .Take(Setting.PostLimitPerWindow.Value) // limit count
                                                           .LastOrDefault();
                    if (oldest == null)
                    {
                        IsFallbacked = false;
                        FallbackPredictedReleaseTime = DateTime.Now;
                    }
                    else
                    {
                        FallbackPredictedReleaseTime = oldest.CreatedAt +
                                                       TimeSpan.FromSeconds(Setting.PostWindowTimeSec.Value);
                        // create timer
                        if (_prevScheduled != null)
                        {
                            _prevScheduled.Dispose();
                        }
                        _prevScheduled = Observable.Timer(FallbackPredictedReleaseTime)
                                                   .Subscribe(_ =>
                                                   {
                                                       IsFallbacked = false;
                                                       this.RaiseFallbackStateUpdated();
                                                   });
                    }
                }
                else
                {
                    FallbackPredictedReleaseTime = DateTime.Now;
                    if (_prevScheduled != null)
                    {
                        _prevScheduled.Dispose();
                        _prevScheduled = null;
                    }
                }
                if (IsFallbacked)
                {
                    BackstageModel.RegisterEvent(new PostLimitedEvent(Account, FallbackPredictedReleaseTime));
                }
                this.RaiseFallbackStateUpdated();
            });
        }
    }
}
