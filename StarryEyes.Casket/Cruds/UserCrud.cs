using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
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

        internal override async Task InitializeAsync()
        {
            await base.InitializeAsync();
            await this.CreateIndexAsync("UT_SN", "ScreenName", true);
        }

        public async Task<DatabaseUser> GetAsync(string screenName)
        {
            return (await this.QueryAsync<DatabaseUser>(
                this.CreateSql("LOWER(ScreenName) = @ScreenName limit 1"),
                new { ScreenName = screenName.ToLower() }))
                .SingleOrDefault();
        }

        public long GetId(string screenName)
        {
            var sql = "select Id from " + TableName + " where LOWER(ScreenName) = @ScreenName limit 1;";
            try
            {
                using (this.AcquireReadLock())
                using (var con = this.DangerousOpenConnection())
                {
                    return
                        con.Query<long>(sql, new { ScreenName = screenName.ToLower() })
                           .SingleOrDefault();
                }
            }
            catch (Exception ex)
            {
                throw WrapException(ex, "GetId", sql);
            }
        }

        public async Task<IEnumerable<DatabaseUser>> GetUsersAsync(string partOfScreenName)
        {
            return await this.QueryAsync<DatabaseUser>(
                this.CreateSql("LOWER(ScreenName) like @Match"),
                new { Match = "%" + partOfScreenName.ToLower() + "%" });
        }

        public async Task<IEnumerable<DatabaseUser>> GetUsersFastAsync(string firstMatchScreenName, int count)
        {
            return await this.QueryAsync<DatabaseUser>(
                this.CreateSql("LOWER(ScreenName) like @Match order by ScreenName limit " + count),
                new { Match = firstMatchScreenName.ToLower() + "%" });
        }
    }
}
