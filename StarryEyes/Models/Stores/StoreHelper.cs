using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cadena.Api.Parameters;
using Cadena.Api.Rest;
using Cadena.Data;
using StarryEyes.Models.Databases;
using StarryEyes.Models.Receiving.Handling;
using StarryEyes.Settings;

namespace StarryEyes.Models.Stores
{
    /// <summary>
    /// Find local cache, if not existed, query to Twitter.
    /// </summary>
    public static class StoreHelper
    {
        public static async Task<TwitterStatus> GetTweetAsync(long id)
        {
            var status = await StatusProxy.GetStatusAsync(id).ConfigureAwait(false);
            if (status == null)
            {
                var acc = Setting.Accounts.GetRandomOne();
                if (acc == null) return null;
                status = (await acc.CreateAccessor().ShowTweetAsync(id, CancellationToken.None)
                                   .ConfigureAwait(false)).Result;
                StatusInbox.Enqueue(status);
            }
            return status;
        }

        public static async Task<TwitterUser> GetUserAsync(long id)
        {
            var user = await UserProxy.GetUserAsync(id).ConfigureAwait(false);
            if (user == null)
            {
                var acc = Setting.Accounts.GetRelatedOne(id);
                if (acc == null) return null;

                user = (await acc.CreateAccessor().ShowUserAsync(new UserParameter(id), CancellationToken.None)
                                 .ConfigureAwait(false)).Result;
                UserProxy.StoreUser(user);
            }
            return user;
        }

        public static async Task<TwitterUser> GetUserAsync(string screenName, bool ignoreCache = false)
        {
            var user = ignoreCache ? null : await UserProxy.GetUserAsync(screenName).ConfigureAwait(false);
            if (user == null)
            {
                var acc = Setting.Accounts.GetRandomOne();
                if (acc == null) return null;
                user = (await acc.CreateAccessor().ShowUserAsync(new UserParameter(screenName), CancellationToken.None)
                                 .ConfigureAwait(false)).Result;
                UserProxy.StoreUser(user);
            }
            return user;
        }

        public static async Task<IEnumerable<TwitterUser>> GetUsersAsync(IEnumerable<long> ids)
        {
            var target = ids.ToArray();
            var users = (await UserProxy.GetUsersAsync(target).ConfigureAwait(false)).ToList();
            var needFetch = target.Except(users.Select(u => u.Id));
            foreach (var id in needFetch)
            {
                var acc = Setting.Accounts.GetRelatedOne(id);
                if (acc != null)
                {
                    var user = (await acc.CreateAccessor().ShowUserAsync(new UserParameter(id), CancellationToken.None)
                                         .ConfigureAwait(false)).Result;
                    UserProxy.StoreUser(user);
                    users.Add(user);
                }
            }
            return users;
        }
    }
}