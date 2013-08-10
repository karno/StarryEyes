using System;
using System.Threading.Tasks;
using StarryEyes.Anomaly.TwitterApi.Rest;
using StarryEyes.Models.Accounting;

namespace StarryEyes.Models.Requests
{
    public sealed class DestroySearchQueryRequest : RequestBase<Tuple<long, string>>
    {
        private readonly long _id;

        public DestroySearchQueryRequest(long id)
        {
            _id = id;
        }

        public override Task<Tuple<long, string>> Send(TwitterAccount account)
        {
            return account.DestroySavedSearch(_id);
        }
    }
}
