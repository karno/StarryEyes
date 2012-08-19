using System;
using StarryEyes.Mystique.Models.Store;
using StarryEyes.SweetLady.Api.Rest;
using StarryEyes.SweetLady.Authorize;

namespace StarryEyes.Mystique.Models.Connection.Polling
{
    /// <summary>
    /// receives Home/Mentions/Direct Messages
    /// </summary>
    public class EssentialTimelinesReceiver : PollingConnectionBase
    {
        public EssentialTimelinesReceiver(AuthenticateInfo info)
            : base(info) { }

        protected override int IntervalSec
        {
            // TODO: set this parameter via setting UI.
            get { return 60; }
        }

        protected override void DoReceive()
        {
            ReceiveHomeTimeline(this.AuthInfo);
            ReceiveMentionTimeline(this.AuthInfo);
            ReceiveMessages(this.AuthInfo);
        }

        public static void ReceiveHomeTimeline(AuthenticateInfo info)
        {
            info.GetHomeTimeline(count: 100, include_rts: true, include_entities: true)
                .Subscribe(t => StatusStore.Store(t));
        }

        public static void ReceiveMentionTimeline(AuthenticateInfo info)
        {
            info.GetMentions(count: 100, include_rts: false)
                .Subscribe(t => StatusStore.Store(t));
        }

        public static void ReceiveMessages(AuthenticateInfo info)
        {
            info.GetDirectMessages(count: 50)
                .Subscribe(t => StatusStore.Store(t));
        }
    }
}
