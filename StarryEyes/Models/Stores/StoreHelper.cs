using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StarryEyes.Anomaly.TwitterApi;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.TwitterApi.Rest;
using StarryEyes.Anomaly.TwitterApi.Rest.Parameters;
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
            var status = await StatusProxy.GetStatusAsync(id);
            if (status == null)
            {
                var acc = Setting.Accounts.GetRandomOne();
                if (acc == null) return null;
                status = (await acc.ShowTweetAsync(ApiAccessProperties.Default, id)).Result;
                StatusInbox.Enqueue(status);
            }
            return status;
        }

        public static async Task<TwitterUser> GetUserAsync(long id)
        {
            var user = await UserProxy.GetUserAsync(id);
            if (user == null)
            {
                var acc = Setting.Accounts.GetRelatedOne(id);
                if (acc == null) return null;
                user = (await acc.ShowUserAsync(ApiAccessProperties.Default, new UserParameter(id))).Result;
                UserProxy.StoreUser(user);
            }
            return user;
        }

        public static async Task<TwitterUser> GetUserAsync(string screenName)
        {
            var user = await UserProxy.GetUserAsync(screenName);
            if (user == null)
            {
                var acc = Setting.Accounts.GetRandomOne();
                if (acc == null) return null;
                user = (await acc.ShowUserAsync(ApiAccessProperties.Default, new UserParameter(screenName))).Result;
                UserProxy.StoreUser(user);
            }
            return user;
        }

        public static async Task<IEnumerable<TwitterUser>> GetUsersAsync(IEnumerable<long> ids)
        {
            var target = ids.ToArray();
            var users = (await UserProxy.GetUsersAsync(target)).ToList();
            var needFetch = target.Except(users.Select(u => u.Id));
            foreach (var id in needFetch)
            {
                var acc = Setting.Accounts.GetRelatedOne(id);
                if (acc != null)
                {
                    var user = (await acc.ShowUserAsync(ApiAccessProperties.Default, new UserParameter(id))).Result;
                    UserProxy.StoreUser(user);
                    users.Add(user);
                }
            }
            return users;
        }
    }
}
