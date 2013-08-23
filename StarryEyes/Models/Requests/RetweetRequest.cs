using System;
using System.Threading.Tasks;
using StarryEyes.Anomaly.TwitterApi;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.TwitterApi.Rest;
using StarryEyes.Models.Accounting;
using StarryEyes.Settings;

namespace StarryEyes.Models.Requests
{
    public sealed class RetweetRequest : RequestBase<TwitterStatus>
    {
        private const string LimitMessage =
            "Wow, that's a lot of Twittering! You have reached your limit of updates for the day.";

        private readonly long _id;
        private readonly bool _createRetweet;

        public RetweetRequest(TwitterStatus status, bool createRetweet)
            : this(status.Id, createRetweet)
        {
        }

        public RetweetRequest(long id, bool createRetweet)
        {
            _id = id;
            this._createRetweet = createRetweet;
        }

        public override async Task<TwitterStatus> Send(TwitterAccount account)
        {
            // ReSharper disable RedundantIfElseBlock
            if (_createRetweet)
            {
                Exception thrown;
                // make retweet
                var acc = account;
                do
                {
                    try
                    {
                        return await acc.RetweetAsync(_id);
                    }
                    catch (TwitterApiException tae)
                    {
                        thrown = tae;
                        if (tae.Message.Contains(LimitMessage) &&
                            acc.FallbackAccountId != null)
                        {
                            // reached post limit, fallback
                            acc = Setting.Accounts.Get(acc.FallbackAccountId.Value);
                            continue;
                        }
                    }
                    var id = await acc.GetMyRetweetIdOfStatusAsync(_id);
                    if (id.HasValue)
                    {
                        // already retweeted.
                        return null;
                    }
                    throw thrown;
                } while (acc != null && acc.Id != account.Id);
                throw thrown;
            }
            else
            {
                // get retweet id
                var id = await account.GetMyRetweetIdOfStatusAsync(_id);
                if (!id.HasValue)
                {
                    // retweet is not existed.
                    return null;
                }
                // destroy retweet
                return await account.DestroyAsync(id.Value);
            }
            // ReSharper restore RedundantIfElseBlock
        }
    }
}
