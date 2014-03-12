using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StarryEyes.Anomaly.TwitterApi.DataModels;
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
                return await Database.ListCrud.GetAsync(userId, listInfo.Slug);
            });
        }

        public static async Task<IEnumerable<long>> GetListMembers(ListInfo listInfo)
        {
            var user = await Database.UserCrud.GetAsync(listInfo.OwnerScreenName);
            if (user == null) return Enumerable.Empty<long>();
            var list = await Database.ListCrud.GetAsync(user.Id, listInfo.Slug);
            if (list == null) return Enumerable.Empty<long>();
            return await GetListMembers(list.Id);
        }

        public static async Task<IEnumerable<long>> GetListMembers(long listId)
        {
            return await Database.ListUserCrud.GetUsersAsync(listId);
        }

        public static async Task SetListMembers(TwitterList list, IEnumerable<long> users)
        {
            await Database.ListCrud.InsertAsync(new DatabaseList
            {
                Id = list.Id,
                UserId = list.User.Id,
                Name = list.Name,
                FullName = list.FullName,
                Slug = list.Slug
            });
            await SetListMembers(list.Id, users);
        }

        public static async Task SetListMembers(long listId, IEnumerable<long> users)
        {
            var ua = users.ToArray();
            await Database.ListUserCrud.RegisterUsersAsync(listId, ua);
            await Database.ListUserCrud.RemoveUnspecifiedUsersAsync(listId, ua);
        }
    }
}
