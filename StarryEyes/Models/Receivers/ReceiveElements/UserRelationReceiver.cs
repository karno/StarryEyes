using System;
using System.Linq;
using System.Reactive.Linq;
using StarryEyes.Albireo.Data;
using StarryEyes.Breezy.Api.Rest;
using StarryEyes.Breezy.Authorize;
using StarryEyes.Models.Backstages.NotificationEvents;
using StarryEyes.Models.Stores;
using StarryEyes.Settings;

namespace StarryEyes.Models.Receivers.ReceiveElements
{
    public class UserRelationReceiver : CyclicReceiverBase
    {
        private readonly AuthenticateInfo _authInfo;

        public UserRelationReceiver(AuthenticateInfo authInfo)
        {
            _authInfo = authInfo;
        }

        protected override int IntervalSec
        {
            get { return Setting.UserRelationReceivePeriod.Value; }
        }

        protected override void DoReceive()
        {
            // get relation info
            var reldata = AccountRelationDataStore.Get(_authInfo.Id);
            var beforeFollowing = new AVLTree<long>(reldata.Following);
            var beforeFollowers = new AVLTree<long>(reldata.Followers);
            var beforeBlockings = new AVLTree<long>(reldata.Blockings);
            // get followings / followers
            Observable.Merge(
                _authInfo.GetFriendsIdsAll(_authInfo.Id)
                         .Do(id => beforeFollowing.Remove(id))
                         .Do(reldata.AddFollowing),
                _authInfo.GetFollowerIdsAll(_authInfo.Id)
                         .Do(id => beforeFollowers.Remove(id))
                         .Do(reldata.AddFollower),
                _authInfo.GetBlockingsIdsAll()
                         .Do(id => beforeBlockings.Remove(id))
                         .Do(reldata.AddBlocking))
                      .Subscribe(_ => { },
                                 ex => BackstageModel.RegisterEvent(
                                     new OperationFailedEvent("relation receive error: " +
                                                              _authInfo.UnreliableScreenName + " - " +
                                                              ex.Message)),
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
