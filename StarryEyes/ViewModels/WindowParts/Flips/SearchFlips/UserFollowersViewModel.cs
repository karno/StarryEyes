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
    public class UserFollowersViewModel : UserListViewModelBase
    {
        public UserFollowersViewModel(UserInfoViewModel parent)
            : base(parent)
        {
        }

        protected override string UserListName => SearchFlipResources.MsgUserFollowers;

        protected override async Task<ICursorResult<IEnumerable<long>>> GetUsersApiImpl(TwitterAccount info, long id,
            long cursor)
        {
            return (await info.CreateAccessor()
                              .GetFollowersIdsAsync(new UserParameter(id), cursor, null, CancellationToken.None))
                .Result;
        }
    }
}