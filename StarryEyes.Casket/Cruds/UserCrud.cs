using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using StarryEyes.Casket.Connections;
using StarryEyes.Casket.Cruds.Scaffolding;
using StarryEyes.Casket.DatabaseModels;

namespace StarryEyes.Casket.Cruds
{
    public sealed class UserCrud : CrudBase<DatabaseUser>
    {
        public UserCrud()
            : base(ResolutionMode.Replace)
        {
        }

        internal override async Task InitializeAsync(IDatabaseConnectionDescriptor descriptor)
        {
            await base.InitializeAsync(descriptor).ConfigureAwait(false);
            await CreateIndexAsync("UT_SN", "ScreenName", true).ConfigureAwait(false);
        }

        public async Task<DatabaseUser> GetAsync(string screenName)
        {
            return (await Descriptor.QueryAsync<DatabaseUser>(
                CreateSql("LOWER(ScreenName) = @ScreenName limit 1"),
                new { ScreenName = screenName.ToLower() }).ConfigureAwait(false))
                .SingleOrDefault();
        }

        public long GetId(string screenName)
        {
            // synchronized read
            var sql = "select Id from " + TableName + " where LOWER(ScreenName) = @ScreenName limit 1;";
            try
            {
                using (Descriptor.AcquireReadLock())
                using (var con = Descriptor.GetConnection())
                {
                    return con.Query<long>(sql, new { ScreenName = screenName.ToLower() })
                              .SingleOrDefault();
                }
            }
            catch (Exception ex)
            {
                throw DatabaseConnectionHelper.WrapException(ex, "GetId", sql);
            }
        }

        public Task<IEnumerable<DatabaseUser>> GetUsersAsync(IEnumerable<long> ids)
        {
            return Descriptor.QueryAsync<DatabaseUser>(
                CreateSql("Id IN @Ids"),
                new { Ids = ids.ToArray() });
        }

        public Task<IEnumerable<DatabaseUser>> GetUsersAsync(string partOfScreenName)
        {
            return Descriptor.QueryAsync<DatabaseUser>(
                CreateSql("LOWER(ScreenName) like @Match"),
                new { Match = "%" + partOfScreenName.ToLower() + "%" });
        }

        public Task<IEnumerable<DatabaseUser>> GetUsersFastAsync(string firstMatchScreenName, int count)
        {
            return Descriptor.QueryAsync<DatabaseUser>(
                CreateSql(string.Format("LOWER(ScreenName) like @Match order by ScreenName limit {0}", count)),
                new { Match = firstMatchScreenName.ToLower() + "%" });
        }

        public Task<IEnumerable<DatabaseUser>> GetRelatedUsersFastAsync(
            string firstMatchScreenName, bool followingsOnly, int count)
        {
            var targetTables = followingsOnly
                ? new[] { "Followings" }
                : new[] { "Followings", "Followers" };
            var union = targetTables.Select(t => "select distinct TargetId from " + t)
                                    .JoinString(" union ");

            return Descriptor.QueryAsync<DatabaseUser>(
                "select * " +
                "from " + TableName + " " +
                "inner join (" + union + ") on Id = TargetId " +
                "where LOWER(ScreenName) like @Match order by ScreenName limit " + count + ";",
                new { Match = firstMatchScreenName.ToLower() + "%" });
        }
    }
}
