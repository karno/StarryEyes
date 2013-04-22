using System;
using StarryEyes.Breezy.Api.Rest;
using StarryEyes.Breezy.Authorize;
using StarryEyes.Models.Backpanels.NotificationEvents;
using StarryEyes.Settings;

namespace StarryEyes.Models.Receivers.ReceiveElements
{
    public class HomeTimelineReceiver : CyclicReceiverBase
    {
        private readonly AuthenticateInfo _authInfo;

        public HomeTimelineReceiver(AuthenticateInfo authInfo)
        {
            _authInfo = authInfo;
        }

        protected override int IntervalSec
        {
            get { return Setting.RESTReceivePeriod.Value; }
        }

        protected override void DoReceive()
        {
            _authInfo.GetHomeTimeline(count: 100, include_rts: true, include_entities: true)
                     .Subscribe(ReceiveInbox.Queue,
                                ex => BackpanelModel.RegisterEvent(
                                    new OperationFailedEvent("home timeline receive error: " +
                                                             _authInfo.UnreliableScreenName + " - " +
                                                             ex.Message)));
        }
    }
}
