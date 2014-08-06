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
            await base.InitializeAsync(descriptor);
            await this.CreateIndexAsync("UT_SN", "ScreenName", true);
        }

        public async Task<DatabaseUser> GetAsync(string screenName)
        {
            return (await Descriptor.QueryAsync<DatabaseUser>(
                this.CreateSql("LOWER(ScreenName) = @ScreenName limit 1"),
                new { ScreenName = screenName.ToLower() }))
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

        public async Task<IEnumerable<DatabaseUser>> GetUsersAsync(string partOfScreenName)
        {
            return await Descriptor.QueryAsync<DatabaseUser>(
                this.CreateSql("LOWER(ScreenName) like @Match"),
                new { Match = "%" + partOfScreenName.ToLower() + "%" });
        }

        public async Task<IEnumerable<DatabaseUser>> GetUsersFastAsync(string firstMatchScreenName, int count)
        {
            return await Descriptor.QueryAsync<DatabaseUser>(
                this.CreateSql("LOWER(ScreenName) like @Match order by ScreenName limit " + count),
                new { Match = firstMatchScreenName.ToLower() + "%" });
        }

        public async Task<IEnumerable<DatabaseUser>> GetRelatedUsersFastAsync(
            string firstMatchScreenName, bool followingsOnly, int count)
        {
            var targetTables = followingsOnly
                ? new[] { "Followings" }
                : new[] { "Followings", "Followers" };
            var union = targetTables.Select(t => "select distinct TargetId from " + t)
                                    .JoinString(" union ");

            return await Descriptor.QueryAsync<DatabaseUser>(
                "select * " +
                "from " + this.TableName + " " +
                "inner join (" + union + ") on Id = TargetId " +
                "where LOWER(ScreenName) like @Match order by ScreenName limit " + count + ";",
                new { Match = firstMatchScreenName.ToLower() + "%" });
        }
    }
}
