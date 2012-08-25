using System;
using StarryEyes.Mystique.Models.Store;
using StarryEyes.SweetLady.Api.Rest;
using StarryEyes.SweetLady.Authorize;

namespace StarryEyes.Mystique.Models.Connection.Essentials
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
            get { return 20; }
        }

        private int latch = 0;
        protected override void DoReceive()
        {
            latch = (latch + 1) % 3;
            switch (latch)
            {
                case 0:
                    ReceiveHomeTimeline(this.AuthInfo);
                    break;
                case 1:
                    ReceiveMentionTimeline(this.AuthInfo);
                    break;
                case 2:
                    ReceiveMessages(this.AuthInfo);
                    break;
            }
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
