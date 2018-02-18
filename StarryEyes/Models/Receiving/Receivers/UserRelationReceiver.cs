using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using StarryEyes.Anomaly.TwitterApi.Rest;
using StarryEyes.Anomaly.Utils;
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
                this._account.RetrieveAllCursor((a, c) => a.GetFriendsIdsAsync(this._account.Id, c))
                    .Do(newFollowings.Add),
                this._account.RetrieveAllCursor((a, c) => a.GetFollowersIdsAsync(this._account.Id, c))
                    .Do(newFollowers.Add),
                this._account.RetrieveAllCursor((a, c) => a.GetBlockingsIdsAsync(c))
                    .Do(newBlockings.Add),
                this._account.GetNoRetweetsIdsAsync().ToObservable()
                    .Do(newNoRetweets.Add),
                this._account.RetrieveAllCursor((c, i) => c.GetMuteIdsAsync(i))
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
