using System;
using System.Linq;
using System.Reactive.Linq;
using StarryEyes.Albireo.Data;
using StarryEyes.Anomaly.TwitterApi.Rest;
using StarryEyes.Models.Accounting;
using StarryEyes.Models.Backstages.NotificationEvents;
using StarryEyes.Settings;

namespace StarryEyes.Models.Receivers.ReceiveElements
{
    public class UserRelationReceiver : CyclicReceiverBase
    {
        private readonly TwitterAccount _account;

        public UserRelationReceiver(TwitterAccount account)
        {
            this._account = account;
        }

        protected override int IntervalSec
        {
            get { return Setting.UserRelationReceivePeriod.Value; }
        }

        protected override void DoReceive()
        {
            // get relation account
            var reldata = _account.RelationData;
            var beforeFollowing = new AVLTree<long>(reldata.Following);
            var beforeFollowers = new AVLTree<long>(reldata.Followers);
            var beforeBlockings = new AVLTree<long>(reldata.Blockings);
            // get followings / followers
            Observable.Merge(
                this._account.RetrieveAllCursor((a, c) => a.GetFriendsIdsAsync(_account.Id, c))
                    .Do(id => beforeFollowing.Remove(id))
                    .Do(reldata.AddFollowing),
                this._account.RetrieveAllCursor((a, c) => a.GetFollowersIdsAsync(_account.Id, c))
                    .Do(id => beforeFollowers.Remove(id))
                    .Do(reldata.AddFollower),
                this._account.RetrieveAllCursor((a, c) => a.GetBlockingsIdsAsync(c))
                    .Do(id => beforeBlockings.Remove(id))
                    .Do(reldata.AddBlocking))
                      .Subscribe(_ => { },
                                 ex =>
                                 {
                                     BackstageModel.RegisterEvent(
                                         new OperationFailedEvent("relation receive error: " +
                                                                  this._account.UnreliableScreenName + " - " +
                                                                  ex.Message));
                                     System.Diagnostics.Debug.WriteLine(ex);
                                 },
                                 () =>
                                 {
                                     // cleanup remains
                                     beforeFollowing.ForEach(reldata.RemoveFollowing);
                                     beforeFollowers.ForEach(reldata.RemoveFollower);
                                     beforeBlockings.ForEach(reldata.RemoveBlocking);
                                 });
        }
    }
}
