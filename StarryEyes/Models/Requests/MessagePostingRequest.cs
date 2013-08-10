using System.Threading.Tasks;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.TwitterApi.Rest;
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
            return account.SendDirectMessage(_id, _text);
        }
    }
}
