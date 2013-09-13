using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Casket;

namespace StarryEyes.Models.Databases
{
    public static class DatabaseProxy
    {
        public static async Task StoreStatusAsync(TwitterStatus status)
        {
            if (status.RetweetedOriginal != null)
            {
                await StoreStatusAsync(status.RetweetedOriginal);
            }
            var mapped = Mapper.Map(status);
            try
            {
                if (await Database.StatusCrud.GetAsync(status.Id) == null)
                {
                    await Database.StoreStatus(mapped.Item1, mapped.Item2, mapped.Item3);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        public static async Task<IEnumerable<long>> GetRetweetedStatusIds(long originalId)
        {
            try
            {
                var rts = await Database.StatusCrud.GetRetweetedStatusesAsync(originalId);
                return rts.Select(r => r.Id);
            }
            catch
            {
                return Enumerable.Empty<long>();
            }
        }

        public static async Task RemoveStatusAsync(long statusId)
        {
            // remove retweets
            await Database.StatusCrud.DeleteAsync(statusId);
        }

        public static async Task AddFavoritorAsync(long statusId, long userId)
        {
            try
            {
                await Database.FavoritesCrud.InsertAsync(statusId, userId);
            }
            catch { }
        }

        public static async Task RemoveFavoritorAsync(long statusId, long userId)
        {
            try
            {
                await Database.FavoritesCrud.RemoveWhereAsync(statusId, userId);
            }
            catch { }
        }

        public static async Task AddRetweeterAsync(long statusId, long userId)
        {
            try
            {
                await Database.RetweetsCrud.InsertAsync(statusId, userId);
            }
            catch { }
        }

        public static async Task RemoveRetweeterAsync(long statusId, long userId)
        {
            try
            {
                await Database.RetweetsCrud.RemoveWhereAsync(statusId, userId);
            }
            catch { }
        }
    }
}
