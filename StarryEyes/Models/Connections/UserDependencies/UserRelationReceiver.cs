using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using StarryEyes.Albireo.Data;
using StarryEyes.Breezy.Api.Rest;
using StarryEyes.Breezy.Authorize;
using StarryEyes.Models.Backpanels.SystemEvents;
using StarryEyes.Models.Stores;
using StarryEyes.Settings;

namespace StarryEyes.Models.Connections.UserDependencies
{
    public sealed class UserRelationReceiver : PollingConnectionBase
    {
        public UserRelationReceiver(AuthenticateInfo ai)
            : base(ai)
        {
        }

        protected override int IntervalSec
        {
            get { return Setting.UserRelationReceivePeriod.Value; }
        }

        protected override void DoReceive()
        {
            // get relation info
            var reldata = AccountRelationDataStore.Get(this.AuthInfo.Id);
            var beforeFollowing = new AVLTree<long>(reldata.Followings);
            var beforeFollowers = new AVLTree<long>(reldata.Followers);
            var beforeBlockings = new AVLTree<long>(reldata.Blockings);
            // get followings / followers
            Observable.Merge(
                this.AuthInfo.GetFriendsIdsAll(this.AuthInfo.Id)
                    .Do(id => beforeFollowing.Remove(id))
                    .Do(reldata.AddFollowing),
                this.AuthInfo.GetFollowerIdsAll(this.AuthInfo.Id)
                    .Do(id => beforeFollowers.Remove(id))
                    .Do(reldata.AddFollower),
                this.AuthInfo.GetBlockingsIdsAll()
                    .Do(id => beforeBlockings.Remove(id))
                    .Do(reldata.AddBlocking))
                      .Subscribe(_ => { },
                      ex => BackpanelModel.RegisterEvent(
                          new OperationFailedEvent("relation information receive failed: " + ex.Message)),
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
