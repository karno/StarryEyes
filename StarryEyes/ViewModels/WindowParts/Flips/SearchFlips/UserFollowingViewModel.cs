using System.Collections.Generic;
using System.Threading.Tasks;
using StarryEyes.Anomaly.TwitterApi.Rest;
using StarryEyes.Anomaly.TwitterApi.Rest.Infrastructure;
using StarryEyes.Models.Accounting;

namespace StarryEyes.ViewModels.WindowParts.Flips.SearchFlips
{
    public class UserFollowingViewModel : UserListViewModelBase
    {
        public UserFollowingViewModel(UserInfoViewModel parent)
            : base(parent)
        {
        }

        protected override Task<ICursorResult<IEnumerable<long>>> GetUsersApiImpl(TwitterAccount info, long id, long cursor)
        {
            return info.GetFriendsIds(id, cursor);
        }
    }
}
