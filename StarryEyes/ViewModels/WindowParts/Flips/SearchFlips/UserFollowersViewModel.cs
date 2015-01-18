using System.Collections.Generic;
using System.Threading.Tasks;
using StarryEyes.Anomaly.TwitterApi.Rest;
using StarryEyes.Anomaly.TwitterApi.Rest.Infrastructure;
using StarryEyes.Anomaly.TwitterApi.Rest.Parameter;
using StarryEyes.Globalization.WindowParts;
using StarryEyes.Models.Accounting;

namespace StarryEyes.ViewModels.WindowParts.Flips.SearchFlips
{
    public class UserFollowersViewModel : UserListViewModelBase
    {
        public UserFollowersViewModel(UserInfoViewModel parent)
            : base(parent)
        {
        }

        protected override string UserListName
        {
            get { return SearchFlipResources.MsgUserFollowers; }
        }

        protected override Task<ICursorResult<IEnumerable<long>>> GetUsersApiImpl(TwitterAccount info, long id, long cursor)
        {
            return info.GetFollowersIdsAsync(new UserParameter(id), cursor);
        }
    }
}