using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Starcluster.Connections;
using Starcluster.Infrastructures;
using Starcluster.Models;

namespace Starcluster.Tables
{
    public class UserTable : TableBase<DbTwitterUser>
    {
        public UserTable(string tableName)
            : base(tableName, ResolutionMode.Replace)
        {
        }

        public override async Task InitializeAsync(IDatabaseConnectionDescriptor descriptor)
        {
            await base.InitializeAsync(descriptor).ConfigureAwait(false);
            await CreateIndexAsync("UT_SN", nameof(DbTwitterUser.ScreenName), true).ConfigureAwait(false);
        }

        public async Task<DbTwitterUser> GetAsync(string screenName)
        {
            return (await Descriptor.QueryAsync<DbTwitterUser>(
                    CreateSql($"LOWER({nameof(DbTwitterUser.ScreenName)}) = @ScreenName LIMIT 1"),
                    new {ScreenName = screenName.ToLower()}).ConfigureAwait(false))
                .SingleOrDefault();
        }

        public long GetId(string screenName)
        {
            // synchronized read
            var sql =
                $"SELECT Id FROM {TableName} WHERE LOWER({nameof(DbTwitterUser.ScreenName)}) = @ScreenName LIMIT 1;";
            try
            {
                using (Descriptor.AcquireReadLock())
                using (var con = Descriptor.GetConnection())
                {
                    return con.Query<long>(sql, new {ScreenName = screenName.ToLower()})
                              .SingleOrDefault();
                }
            }
            catch (Exception ex)
            {
                throw new DatabaseAccessException(ex, "GetId", sql);
            }
        }

        public Task<IEnumerable<DbTwitterUser>> GetUsersAsync(IEnumerable<long> ids)
        {
            return Descriptor.QueryAsync<DbTwitterUser>(
                CreateSql("Id IN @Ids"),
                new {Ids = ids.ToArray()});
        }

        public Task<IEnumerable<DbTwitterUser>> GetUsersAsync(string partOfScreenName)
        {
            return Descriptor.QueryAsync<DbTwitterUser>(
                CreateSql($"LOWER({nameof(DbTwitterUser.ScreenName)}) LIKE @Match"),
                new {Match = "%" + partOfScreenName.ToLower() + "%"});
        }

        public Task<IEnumerable<DbTwitterUser>> GetUsersFastAsync(string firstMatchScreenName, int count)
        {
            return Descriptor.QueryAsync<DbTwitterUser>(
                CreateSql($"LOWER({nameof(DbTwitterUser.ScreenName)}) LIKE @Match " +
                          $"ORDER BY {nameof(DbTwitterUser.ScreenName)} LIMIT {count}"),
                new {Match = firstMatchScreenName.ToLower() + "%"});
        }

        public Task<IEnumerable<DbTwitterUser>> GetRelatedUsersFastAsync(
            string firstMatchScreenName, bool followingsOnly, int count)
        {
            var targetTables = followingsOnly
                ? new[] {"Followings"}
                : new[] {"Followings", "Followers"};
            var union = String.Join(" UNION ", targetTables.Select(t => "SELECT DISTINCT TargetId FROM " + t));

            return Descriptor.QueryAsync<DbTwitterUser>(
                $"SELECT * FROM {TableName} INNER JOIN ({union}) ON " +
                $"Id = TargetId WHERE LOWER({nameof(DbTwitterUser.ScreenName)}) LIKE @Match " +
                $"ORDER BY ScreenName LIMIT {count};",
                new {Match = firstMatchScreenName.ToLower() + "%"});
        }
    }
}