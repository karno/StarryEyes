using System;
using StarryEyes.Breezy.Api.Rest;
using StarryEyes.Breezy.Authorize;
using StarryEyes.Models.Backpanels.NotificationEvents;
using StarryEyes.Settings;

namespace StarryEyes.Models.Receivers.ReceiveElements
{
    public class MentionTimelineReceiver : CyclicReceiverBase
    {
        private readonly AuthenticateInfo _authInfo;

        public MentionTimelineReceiver(AuthenticateInfo authInfo)
        {
            _authInfo = authInfo;
        }

        protected override int IntervalSec
        {
            get { return Setting.RESTReceivePeriod.Value; }
        }

        protected override void DoReceive()
        {
            _authInfo.GetMentions(count: 100)
                     .Subscribe(ReceiveInbox.Queue,
                                ex => BackpanelModel.RegisterEvent(
                                    new OperationFailedEvent("mentions receive error: " +
                                                             _authInfo.UnreliableScreenName + " - " +
                                                             ex.Message)));
        }
    }
}
