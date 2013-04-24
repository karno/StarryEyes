using System;
using StarryEyes.Breezy.Api.Rest;
using StarryEyes.Breezy.Authorize;
using StarryEyes.Models.Backstages.NotificationEvents;
using StarryEyes.Settings;

namespace StarryEyes.Models.Receivers.ReceiveElements
{
    public class DirectMessagesReceiver : CyclicReceiverBase
    {
        private readonly AuthenticateInfo _authInfo;

        public DirectMessagesReceiver(AuthenticateInfo authInfo)
        {
            _authInfo = authInfo;
        }

        protected override int IntervalSec
        {
            get { return Setting.RESTReceivePeriod.Value; }
        }

        protected override void DoReceive()
        {
            _authInfo.GetDirectMessages(count: 50)
                     .Subscribe(ReceiveInbox.Queue,
                                ex => BackstageModel.RegisterEvent(
                                    new OperationFailedEvent("messages receive error: " +
                                                             _authInfo.UnreliableScreenName + " - " +
                                                             ex.Message)));
            _authInfo.GetSentDirectMessages(count: 50)
                     .Subscribe(ReceiveInbox.Queue,
                                ex => BackstageModel.RegisterEvent(
                                    new OperationFailedEvent("sent messages receive error: " +
                                                             _authInfo.UnreliableScreenName + " - " +
                                                             ex.Message)));
        }
    }
}
