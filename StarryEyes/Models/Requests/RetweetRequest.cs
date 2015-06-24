using System;
using System.Threading.Tasks;
using StarryEyes.Anomaly.TwitterApi;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.TwitterApi.Rest;
using StarryEyes.Models.Accounting;
using StarryEyes.Models.Backstages.NotificationEvents.PostEvents;
using StarryEyes.Settings;

namespace StarryEyes.Models.Requests
{
    public sealed class RetweetRequest : RequestBase<TwitterStatus>
    {
        private const string LimitMessage =
            "You have reached your limit of updates";

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

        public override async Task<IApiResult<TwitterStatus>> Send(TwitterAccount account)
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
                        var result = await acc.RetweetAsync(ApiAccessProperties.Default, _id);
                        BackstageModel.NotifyFallbackState(acc, false);
                        return result;
                    }
                    catch (TwitterApiException tae)
                    {
                        thrown = tae;
                        if (tae.Message.Contains(LimitMessage))
                        {
                            BackstageModel.NotifyFallbackState(acc, true);
                            if (acc.FallbackAccountId != null)
                            {
                                // reached post limit, fallback
                                var prev = acc;
                                acc = Setting.Accounts.Get(acc.FallbackAccountId.Value);
                                BackstageModel.RegisterEvent(new FallbackedEvent(prev, acc));
                                continue;
                            }
                        }
                    }
                    var id = (await acc.GetMyRetweetIdOfStatusAsync(ApiAccessProperties.Default, _id)).Result;
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
                var id = (await account.GetMyRetweetIdOfStatusAsync(ApiAccessProperties.Default, _id)).Result;
                if (!id.HasValue)
                {
                    // retweet is not existed.
                    return null;
                }
                // destroy retweet
                return await account.DestroyAsync(ApiAccessProperties.Default, id.Value);
            }
            // ReSharper restore RedundantIfElseBlock
        }
    }
}
