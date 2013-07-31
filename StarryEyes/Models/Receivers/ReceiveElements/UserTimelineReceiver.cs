using System;
using StarryEyes.Breezy.Api.Rest;
using StarryEyes.Breezy.Authorize;
using StarryEyes.Models.Backstages.NotificationEvents;
using StarryEyes.Settings;

namespace StarryEyes.Models.Receivers.ReceiveElements
{
    public class UserTimelineReceiver : CyclicReceiverBase
    {
        private readonly AuthenticateInfo _authInfo;

        public UserTimelineReceiver(AuthenticateInfo authInfo)
        {
            _authInfo = authInfo;
        }

        protected override int IntervalSec
        {
            get { return Setting.RESTReceivePeriod.Value; }
        }

        protected override void DoReceive()
        {
            _authInfo.GetUserTimeline(_authInfo.Id, count: 100)
                     .Subscribe(ReceiveInbox.Queue,
                                ex => BackstageModel.RegisterEvent(
                                    new OperationFailedEvent("user status receive error: " +
                                                             _authInfo.UnreliableScreenName + " - " + ex.Message)));
        }
    }
}
