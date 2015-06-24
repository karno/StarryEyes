using System.Threading.Tasks;
using StarryEyes.Anomaly.TwitterApi;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.TwitterApi.Rest;
using StarryEyes.Anomaly.TwitterApi.Rest.Parameters;
using StarryEyes.Models.Accounting;

namespace StarryEyes.Models.Requests
{
    public class UpdateMuteRequest : RequestBase<TwitterUser>
    {
        private readonly long _userId;
        private readonly bool _mute;

        public UpdateMuteRequest(TwitterUser target, bool mute)
            : this(target.Id, mute)
        {
        }

        public UpdateMuteRequest(long userId, bool mute)
        {
            this._userId = userId;
            this._mute = mute;
        }

        public override Task<IApiResult<TwitterUser>> Send(TwitterAccount account)
        {
            return account.UpdateMuteAsync(ApiAccessProperties.Default,
                new UserParameter(_userId), _mute);
        }
    }
}
