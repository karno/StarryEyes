using System.Threading.Tasks;
using StarryEyes.Anomaly.TwitterApi;
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

        public override Task<IApiResult<TwitterSavedSearch>> Send(TwitterAccount account)
        {
            return account.DestroySavedSearchAsync(ApiAccessProperties.Default, _id);
        }
    }
}
