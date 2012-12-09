using System.Threading.Tasks;
using StarryEyes.Breezy.Api.Rest;
using StarryEyes.Breezy.Authorize;
using StarryEyes.Settings;

namespace StarryEyes.Models.Connections.UserDependencies
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
            get { return Setting.RESTReceivePeriod.Value; }
        }

        protected override void DoReceive()
        {
            Task.Run(() => ReceiveHomeTimeline(this.AuthInfo));
            Task.Run(() => ReceiveMentionTimeline(this.AuthInfo));
            Task.Run(() => ReceiveMessages(this.AuthInfo));
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
