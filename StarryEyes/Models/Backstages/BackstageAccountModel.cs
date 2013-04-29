using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StarryEyes.Breezy.Authorize;
using StarryEyes.Breezy.DataModel;
using StarryEyes.Models.Backstages.NotificationEvents;
using StarryEyes.Models.Receivers;
using StarryEyes.Models.Receivers.ReceiveElements;
using StarryEyes.Models.Stores;
using StarryEyes.Models.Subsystems;
using StarryEyes.Settings;

namespace StarryEyes.Models.Backstages
{
    public class BackstageAccountModel
    {
        private readonly AuthenticateInfo _info;
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

        public AuthenticateInfo Info
        {
            get { return this._info; }
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
            get { return PostLimitPredictionService.GetCurrentWindowCount(Info.Id); }
        }

        public BackstageAccountModel(AuthenticateInfo info)
        {
            this._info = info;
            this.UpdateConnectionState();
            StoreHelper.GetUser(_info.Id)
                       .Subscribe(
                           u =>
                           {
                               _user = u;
                               this.OnTwitterUserChanged();
                           },
                           ex => BackstageModel.RegisterEvent(
                               new OperationFailedEvent("Could not receive user info: " +
                                                        this._info.UnreliableScreenName +
                                                        " - " + ex.Message)));
        }

        internal void UpdateConnectionState()
        {
            this.ConnectionState = ReceiversManager.GetConnectionState(this.Info.Id);
        }

        public void Reconnect()
        {
            ReceiversManager.ReconnectUserStreams(this.Info.Id);
        }
    }
}
