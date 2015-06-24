using System.Threading.Tasks;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Models.Accounting;

namespace StarryEyes.Models.Requests
{
    public sealed class GivingTrophyRequest : RequestBase<bool>
    {
        private readonly long _id;

        public GivingTrophyRequest(TwitterStatus status)
            : this(status.Id)
        {
        }

        public GivingTrophyRequest(long id)
        {
            _id = id;
        }

#pragma warning disable 1998
        public override async Task<IApiResult<bool>> Send(TwitterAccount account)
#pragma warning restore 1998
        {
            // not implemented yet.
            return null;
        }
    }
}
