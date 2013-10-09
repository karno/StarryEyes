using System;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Models.Accounting;
using StarryEyes.Models.Backstages.NotificationEvents;
using StarryEyes.Models.Receiving;
using StarryEyes.Models.Receiving.Receivers;
using StarryEyes.Models.Stores;
using StarryEyes.Models.Subsystems;

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
    }
}
