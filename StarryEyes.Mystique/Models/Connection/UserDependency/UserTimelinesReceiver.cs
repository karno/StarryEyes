using System;
using StarryEyes.Mystique.Models.Store;
using StarryEyes.SweetLady.Api.Rest;
using StarryEyes.SweetLady.Authorize;

namespace StarryEyes.Mystique.Models.Connection.UserDependency
{
    /// <summary>
    /// receives Home/Mentions/Direct Messages
    /// </summary>
    public class UserTimelinesReceiver : PollingConnectionBase
    {
        public UserTimelinesReceiver(AuthenticateInfo info)
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

        public static void ReceiveHomeTimeline(AuthenticateInfo info, long? max_id = null)
        {
            info.GetHomeTimeline(count: 100, include_rts: true, include_entities: true, max_id: max_id)
                .RegisterToStore();
        }

        public static void ReceiveMentionTimeline(AuthenticateInfo info, long? max_id = null)
        {
            info.GetMentions(count: 100, include_rts: false, max_id: max_id)
                .RegisterToStore();
        }

        public static void ReceiveMessages(AuthenticateInfo info, long? max_id = null)
        {
            info.GetDirectMessages(count: 50, max_id: max_id)
                .RegisterToStore();
        }

    }
}
