using System.Threading.Tasks;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.TwitterApi.Rest;
using StarryEyes.Models.Accounting;

namespace StarryEyes.Models.Requests
{
    public sealed class DeletionRequest : RequestBase<TwitterStatus>
    {
        private readonly long _id;
        private readonly StatusType _type;

        public DeletionRequest(TwitterStatus status)
            : this(status.Id, status.StatusType)
        {
        }

        public DeletionRequest(long id, StatusType type)
        {
            _id = id;
            _type = type;
        }

        public override Task<TwitterStatus> Send(TwitterAccount account)
        {
            return this._type == StatusType.Tweet
                       ? account.DestroyAsync(this._id)
                       : account.DestroyDirectMessageAsync(this._id);
        }
    }
}
