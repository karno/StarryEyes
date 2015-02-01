using System.Threading.Tasks;
using StarryEyes.Anomaly.TwitterApi;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.TwitterApi.Rest;
using StarryEyes.Anomaly.TwitterApi.Rest.Parameters;
using StarryEyes.Models.Accounting;

namespace StarryEyes.Models.Requests
{
    public sealed class MessagePostingRequest : RequestBase<TwitterStatus>
    {
        private readonly long _id;
        private readonly string _text;

        public MessagePostingRequest(TwitterUser target, string text)
            : this(target.Id, text)
        {
        }

        public MessagePostingRequest(long id, string text)
        {
            _id = id;
            _text = text;
        }

        public override Task<TwitterStatus> Send(TwitterAccount account)
        {
            return account.SendDirectMessageAsync(ApiAccessProperties.Default, new UserParameter(_id), _text);
        }
    }
}
