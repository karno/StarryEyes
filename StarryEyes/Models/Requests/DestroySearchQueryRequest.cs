using System.Threading.Tasks;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.TwitterApi.Rest;
using StarryEyes.Models.Accounting;

namespace StarryEyes.Models.Requests
{
    public sealed class DestroySearchQueryRequest : RequestBase<TwitterSavedSearch>
    {
        private readonly long _id;

        public DestroySearchQueryRequest(long id)
        {
            _id = id;
        }

        public override Task<TwitterSavedSearch> Send(TwitterAccount account)
        {
            return account.DestroySavedSearchAsync(_id);
        }
    }
}
