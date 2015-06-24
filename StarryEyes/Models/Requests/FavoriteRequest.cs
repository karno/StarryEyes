using System.Threading.Tasks;
using StarryEyes.Anomaly.TwitterApi;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.TwitterApi.Rest;
using StarryEyes.Models.Accounting;
using StarryEyes.Settings;

namespace StarryEyes.Models.Requests
{
    public sealed class FavoriteRequest : RequestBase<TwitterStatus>
    {
        private const string AlreadyFavorited = "You have already favorited this status";

        private const string LimitMessage =
            "It's great that you like so many updates, but we only allow so many updates to be marked as a favorite per day.";

        private readonly long _id;
        private readonly bool _add;

        public FavoriteRequest(TwitterStatus status, bool add)
            : this(status.Id, add)
        {
        }

        public FavoriteRequest(long id, bool add)
        {
            _id = id;
            _add = add;
        }

        public override async Task<IApiResult<TwitterStatus>> Send(TwitterAccount account)
        {
            var acc = account;
            do
            {
                try
                {
                    return await (this._add
                                      ? acc.CreateFavoriteAsync(ApiAccessProperties.Default, this._id)
                                      : acc.DestroyFavoriteAsync(ApiAccessProperties.Default, this._id));
                }
                catch (TwitterApiException tae)
                {
                    // fallback favorite
                    if (tae.Message.Contains(LimitMessage) &&
                        acc.FallbackAccountId != null &&
                        acc.IsFallbackFavorite)
                    {
                        acc = Setting.Accounts.Get(acc.FallbackAccountId.Value);
                        if (acc != null && acc.Id != account.Id)
                        {
                            continue;
                        }
                    }
                    // check favorite duplication.
                    // (if you destroy unexisted favorite, twitter returns normal response.)
                    if (tae.Message.Contains(AlreadyFavorited))
                    {
                        // already favorited.
                        return null;
                    }
                    throw;
                }
            } while (true);
        }
    }
}
