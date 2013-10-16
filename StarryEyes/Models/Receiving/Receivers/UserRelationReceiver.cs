using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using StarryEyes.Albireo.Collections;
using StarryEyes.Anomaly.TwitterApi.Rest;
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

        protected override int IntervalSec
        {
            get { return Setting.UserRelationReceivePeriod.Value; }
        }

        protected override void DoReceive()
        {
            // get relation account
            var reldata = this._account.RelationData;
            var beforeFollowings = new AVLTree<long>(reldata.Followings);
            var beforeFollowers = new AVLTree<long>(reldata.Followers);
            var beforeBlockings = new AVLTree<long>(reldata.Blockings);
            var newFollowings = new List<long>();
            var newFollowers = new List<long>();
            var newBlockings = new List<long>();
            // get followings / followers
            Observable.Merge(
                this._account.RetrieveAllCursor((a, c) => a.GetFriendsIdsAsync(this._account.Id, c))
                    .Do(id => this.CheckRemove(id, beforeFollowings, newFollowings)),
                this._account.RetrieveAllCursor((a, c) => a.GetFollowersIdsAsync(this._account.Id, c))
                    .Do(id => this.CheckRemove(id, beforeFollowers, newFollowers)),
                this._account.RetrieveAllCursor((a, c) => a.GetBlockingsIdsAsync(c))
                    .Do(id => this.CheckRemove(id, beforeBlockings, newBlockings)))
                      .Subscribe(_ => { },
                                 ex =>
                                 {
                                     BackstageModel.RegisterEvent(
                                         new OperationFailedEvent("relation receive error: " +
                                                                  this._account.UnreliableScreenName + " - " +
                                                                  ex.Message));
                                     System.Diagnostics.Debug.WriteLine(ex);
                                 },
                                 () => Task.Run(async () =>
                                 {
                                     await reldata.RemoveFollowingsAsync(beforeFollowings);
                                     await reldata.AddFollowingsAsync(newFollowings);
                                     await reldata.RemoveFollowersAsync(beforeFollowers);
                                     await reldata.AddFollowersAsync(newFollowers);
                                     await reldata.RemoveBlockingsAsync(beforeBlockings);
                                     await reldata.AddBlockingsAsync(newBlockings);
                                 }));
        }

        private void CheckRemove(long id, AVLTree<long> prevList, List<long> newList)
        {
            // if fails removing
            if (!prevList.Remove(id))
            {
                // that's new item
                newList.Add(id);
            }
        }
    }
}
