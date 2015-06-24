using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using StarryEyes.Anomaly.TwitterApi;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.TwitterApi.Rest;
using StarryEyes.Anomaly.TwitterApi.Rest.Parameters;
using StarryEyes.Globalization;
using StarryEyes.Globalization.Models;
using StarryEyes.Models.Accounting;
using StarryEyes.Settings;

namespace StarryEyes.Models.Receiving.Receivers
{
    public class UserRelationReceiver : CyclicReceiverBase
    {
        private readonly TwitterAccount _account;

        public UserRelationReceiver(TwitterAccount account)
        {
            this._account = account;
        }

        protected override string ReceiverName
        {
            get { return ReceivingResources.ReceiverRelationFormat.SafeFormat("@" + _account.UnreliableScreenName); }
        }

        protected override int IntervalSec
        {
            get { return Setting.UserRelationReceivePeriod.Value; }
        }

        protected override async Task DoReceive()
        {
            // get relation account
            var reldata = this._account.RelationData;
            var newFollowings = new List<long>();
            var newFollowers = new List<long>();
            var newBlockings = new List<long>();
            var newNoRetweets = new List<long>();
            var newMutes = new List<long>();

            // get followings / followers
            await Observable.Merge(
                _account.RetrieveAllCursor((a, c) => a.GetFriendsIdsAsync(ApiAccessProperties.Default, new UserParameter(_account.Id), c))
                    .Do(newFollowings.Add),
                _account.RetrieveAllCursor((a, c) => a.GetFollowersIdsAsync(ApiAccessProperties.Default, new UserParameter(_account.Id), c))
                    .Do(newFollowers.Add),
                _account.RetrieveAllCursor((a, c) => a.GetBlockingsIdsAsync(ApiAccessProperties.Default, c))
                    .Do(newBlockings.Add),
                _account.GetNoRetweetsIdsAsync(ApiAccessProperties.Default).ToObservable().SelectMany(s => s.Result)
                    .Do(newNoRetweets.Add),
                _account.RetrieveAllCursor((c, i) => c.GetMuteIdsAsync(ApiAccessProperties.Default, i))
                    .Do(newMutes.Add)
                ).ToTask();

            // update relation data after receiving relations are completed.
            await Task.WhenAll(
                reldata.Followings.SetAsync(newFollowings),
                reldata.Followers.SetAsync(newFollowers),
                reldata.Blockings.SetAsync(newBlockings),
                reldata.NoRetweets.SetAsync(newNoRetweets),
                reldata.Mutes.SetAsync(newMutes));
        }
    }
}
