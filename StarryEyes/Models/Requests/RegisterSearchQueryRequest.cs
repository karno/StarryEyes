using System.Threading.Tasks;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.TwitterApi.Rest;
using StarryEyes.Models.Accounting;

namespace StarryEyes.Models.Requests
{
    public sealed class RegisterSearchQueryRequest : RequestBase<TwitterSavedSearch>
    {
        private readonly string _query;

        public RegisterSearchQueryRequest(string query)
        {
            _query = query;
        }

        public override Task<TwitterSavedSearch> Send(TwitterAccount account)
        {
            return account.SaveSearchAsync(_query);
        }
    }
}
