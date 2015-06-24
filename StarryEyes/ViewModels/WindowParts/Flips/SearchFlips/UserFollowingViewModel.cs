using System.Collections.Generic;
using System.Threading.Tasks;
using StarryEyes.Anomaly.TwitterApi;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.TwitterApi.Rest;
using StarryEyes.Anomaly.TwitterApi.Rest.Parameters;
using StarryEyes.Globalization.WindowParts;
using StarryEyes.Models.Accounting;

namespace StarryEyes.ViewModels.WindowParts.Flips.SearchFlips
{
    public class UserFollowingViewModel : UserListViewModelBase
    {
        public UserFollowingViewModel(UserInfoViewModel parent)
            : base(parent)
        {
        }

        protected override string UserListName
        {
            get { return SearchFlipResources.MsgUserFollowing; }
        }

        protected override async Task<ICursorResult<IEnumerable<long>>> GetUsersApiImpl(TwitterAccount info, long id, long cursor)
        {
            return (await info.GetFriendsIdsAsync(ApiAccessProperties.Default, new UserParameter(id), cursor)).Result;
        }
    }
}
