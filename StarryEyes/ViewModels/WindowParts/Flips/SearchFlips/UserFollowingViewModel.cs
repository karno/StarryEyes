using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cadena.Api.Parameters;
using Cadena.Api.Rest;
using Cadena.Data;
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

        protected override async Task<ICursorResult<IEnumerable<long>>> GetUsersApiImpl(TwitterAccount info, long id,
            long cursor)
        {
            return (await info.CreateAccessor()
                              .GetFriendsIdsAsync(new UserParameter(id), cursor, null, CancellationToken.None)).Result;
        }
    }
}