using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cadena;
using Cadena.Api.Rest;
using Cadena.Data;
using StarryEyes.Casket;
using StarryEyes.Casket.DatabaseModels;
using StarryEyes.Models.Receiving;

namespace StarryEyes.Models.Databases
{
    public static class ListProxy
    {
        public static async Task<DatabaseList> GetListDescription(ListInfo listInfo)
        {
            return await Task.Run(async () =>
            {
                var userId = UserProxy.GetId(listInfo.OwnerScreenName);
                return await Database.ListCrud.GetAsync(userId, listInfo.Slug).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        public static async Task<DatabaseList> GetOrReceiveListDescription(IApiAccessor accessor, ListInfo listInfo)
        {
            return await Task.Run(async () =>
            {
                var userId = UserProxy.GetId(listInfo.OwnerScreenName);
                var dbl = await Database.ListCrud.GetAsync(userId, listInfo.Slug).ConfigureAwait(false);
                if (dbl != null) return dbl;
                var twl = await accessor.ShowListAsync(listInfo.ToParameter(), CancellationToken.None);
                return await SetListDescription(twl.Result).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }


        public static async Task<IEnumerable<long>> GetListMembers(ListInfo listInfo)
        {
            var user = await Database.UserCrud.GetAsync(listInfo.OwnerScreenName).ConfigureAwait(false);
            if (user == null) return Enumerable.Empty<long>();
            var list = await Database.ListCrud.GetAsync(user.Id, listInfo.Slug).ConfigureAwait(false);
            if (list == null) return Enumerable.Empty<long>();
            return await GetListMembers(list.Id).ConfigureAwait(false);
        }

        public static async Task<IEnumerable<long>> GetListMembers(long listId)
        {
            return await Database.ListUserCrud.GetUsersAsync(listId).ConfigureAwait(false);
        }

        public static async Task<DatabaseList> SetListDescription(TwitterList list)
        {
            var dbl = new DatabaseList
            {
                Id = list.Id,
                UserId = list.User.Id,
                Name = list.Name,
                FullName = list.FullName,
                Slug = list.Slug
            };
            await Database.ListCrud.InsertAsync(dbl).ConfigureAwait(false);
            return dbl;
        }

        public static async Task SetListMembers(TwitterList list, IEnumerable<long> users)
        {
            await SetListDescription(list).ConfigureAwait(false);
            await SetListMembers(list.Id, users).ConfigureAwait(false);
        }

        public static async Task SetListMembers(long listId, IEnumerable<long> users)
        {
            var ua = users.ToArray();
            await Database.ListUserCrud.RegisterUsersAsync(listId, ua).ConfigureAwait(false);
            await Database.ListUserCrud.RemoveUnspecifiedUsersAsync(listId, ua).ConfigureAwait(false);
        }
    }
}