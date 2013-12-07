using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using StarryEyes.Anomaly.TwitterApi.Rest;
using StarryEyes.Anomaly.Utils;
using StarryEyes.Models.Accounting;
using StarryEyes.Models.Backstages.NotificationEvents;
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
            get { return "ユーザー関係(@" + _account.UnreliableScreenName + ")"; }
        }

        protected override int IntervalSec
        {
            get { return Setting.UserRelationReceivePeriod.Value; }
        }

        protected override void DoReceive()
        {
            // get relation account
            var reldata = this._account.RelationData;
            var newFollowings = new List<long>();
            var newFollowers = new List<long>();
            var newBlockings = new List<long>();
            var newNoRetweets = new List<long>();
            // get followings / followers
            Observable.Merge(
                this._account.RetrieveAllCursor((a, c) => a.GetFriendsIdsAsync(this._account.Id, c))
                    .Do(newFollowings.Add),
                this._account.RetrieveAllCursor((a, c) => a.GetFollowersIdsAsync(this._account.Id, c))
                    .Do(newFollowers.Add),
                this._account.RetrieveAllCursor((a, c) => a.GetBlockingsIdsAsync(c))
                    .Do(newBlockings.Add),
                this._account.GetNoRetweetsIdsAsync().ToObservable()
                    .Do(newNoRetweets.Add)
                ).Subscribe(_ => { },
                            ex =>
                            {
                                BackstageModel.RegisterEvent(new OperationFailedEvent(
                                    "関係情報の受信に失敗しました(@" + this._account.UnreliableScreenName + ")", ex));
                                System.Diagnostics.Debug.WriteLine(ex);
                            },
                            () => Task.Run(async () =>
                            {
                                System.Diagnostics.Debug.WriteLine("**** USER INFORMATION UPDATED @" + _account.UnreliableScreenName);
                                await Task.WhenAll(
                                    reldata.SetFollowingsAsync(newFollowings),
                                    reldata.SetFollowersAsync(newFollowers),
                                    reldata.SetBlockingsAsync(newBlockings),
                                    reldata.SetNoRetweetsAsync(newNoRetweets)
                                    );
                            }));
        }
    }
}
