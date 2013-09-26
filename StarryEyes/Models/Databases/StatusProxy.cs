﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using StarryEyes.Annotations;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Casket;
using StarryEyes.Casket.DatabaseModels;

namespace StarryEyes.Models.Databases
{
    public static class StatusProxy
    {
        public static async Task StoreStatusAsync([NotNull] TwitterStatus status)
        {
            if (status == null) throw new ArgumentNullException("status");
            if (status.RetweetedOriginal != null)
            {
                await StoreStatusAsync(status.RetweetedOriginal);
            }
            var mappedStatus = Mapper.Map(status);
            var mappedUser = Mapper.Map(status.User);
            try
            {
                if (!(await Database.StatusCrud.CheckExistsAsync(status.Id)))
                {
                    await Database.StoreStatus(
                        mappedStatus.Item1, mappedStatus.Item2,
                        mappedUser.Item1, mappedUser.Item2, mappedUser.Item3);
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
            try
            {
                // remove retweets
                await Database.StatusCrud.DeleteAsync(statusId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        public static async Task AddFavoritorAsync(long statusId, long userId)
        {
            try
            {
                await Database.FavoritesCrud.InsertAsync(statusId, userId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        public static async Task RemoveFavoritorAsync(long statusId, long userId)
        {
            try
            {
                await Database.FavoritesCrud.RemoveWhereAsync(statusId, userId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        public static async Task AddRetweeterAsync(long statusId, long userId)
        {
            try
            {
                await Database.RetweetsCrud.InsertAsync(statusId, userId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        public static async Task RemoveRetweeterAsync(long statusId, long userId)
        {
            try
            {
                await Database.RetweetsCrud.RemoveWhereAsync(statusId, userId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        public static async Task<TwitterStatus> GetStatusAsync(long id)
        {
            var status = await Database.StatusCrud.GetAsync(id);
            if (status == null) return null;
            return await LoadStatusAsync(status);
        }

        public static IObservable<TwitterStatus> LoadStatusesAsync([NotNull] IEnumerable<DatabaseStatus> dbstatuses)
        {
            if (dbstatuses == null) throw new ArgumentNullException("dbstatuses");
            return dbstatuses
                .ToObservable()
                .SelectMany(s => LoadStatusAsync(s).ToObservable());
        }

        public static async Task<TwitterStatus> LoadStatusAsync([NotNull] DatabaseStatus dbstatus)
        {
            if (dbstatus == null) throw new ArgumentNullException("dbstatus");
            var id = dbstatus.Id;
            var user = UserProxy.GetUserAsync(dbstatus.UserId);
            var se = Database.StatusEntityCrud.GetEntitiesAsync(id);
            var favorers = Database.FavoritesCrud.GetUsersAsync(id);
            var retweeters = Database.RetweetsCrud.GetUsersAsync(id);
            if (dbstatus.RetweetOriginalId == null)
            {
                return Mapper.Map(dbstatus, await se, await favorers, await retweeters, await user);
            }
            var rts = GetStatusAsync(dbstatus.RetweetOriginalId.Value);
            return Mapper.Map(dbstatus, await se, await favorers, await retweeters, await rts, await user);
        }
    }
}