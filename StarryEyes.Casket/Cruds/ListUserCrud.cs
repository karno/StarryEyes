using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using StarryEyes.Casket.Cruds.Scaffolding;
using StarryEyes.Casket.DatabaseModels;

namespace StarryEyes.Casket.Cruds
{
    public class ListUserCrud : CrudBase<DatabaseListUser>
    {
        public ListUserCrud()
            : base(ResolutionMode.Ignore)
        {
        }

        internal override async Task InitializeAsync()
        {
            await base.InitializeAsync();
            await this.CreateIndexAsync("ListUserLID", "ListId", false);
            await this.CreateIndexAsync("ListUserUID", "UserId", false);
        }

        public async Task<IEnumerable<long>> GetUsersAsync(long listId)
        {
            return (await this.QueryAsync<DatabaseListUser>(
                CreateSql("ListId = @listId"), new { listId }))
                .Select(l => l.UserId);
        }

        public async Task RegisterUsersAsync(long listId, IEnumerable<long> userIds)
        {
            await WriteTaskFactory.StartNew(() =>
            {
                try
                {
                    ReaderWriterLock.EnterWriteLock();
                    using (var conn = this.DangerousOpenConnection())
                    using (var tran = conn.BeginTransaction(IsolationLevel.ReadCommitted))
                    {
                        foreach (var userId in userIds)
                        {
                            conn.Execute(TableInserter, new DatabaseListUser(listId, userId));
                        }
                        tran.Commit();
                    }
                }
                finally
                {
                    ReaderWriterLock.ExitWriteLock();
                }
            });
        }

        public async Task RegisterUserAsync(long listId, long userId)
        {
            await InsertAsync(new DatabaseListUser(listId, userId));
        }

        public async Task DeleteUsersAsync(long listId, IEnumerable<long> removalUserIds)
        {
            var uids = removalUserIds.Select(i => i.ToString(CultureInfo.InvariantCulture)).JoinString(",");
            if (String.IsNullOrEmpty(uids)) return;
            await this.ExecuteAsync(
                "delete from " + TableName + " where " +
                "ListId = @listId and UserId in (" + uids + ");",
                new { listId });
        }

        public async Task DeleteUserAsync(long listId, long userId)
        {
            await this.ExecuteAsync("delete from " + TableName + "where " +
                                    "ListId = @listId and UserId = @userId", new { listId, userId });
        }

        public async Task RemoveUnspecifiedUsersAsync(long listId, IEnumerable<long> users)
        {
            var uids = users.Select(i => i.ToString(CultureInfo.InvariantCulture)).JoinString(",");
            if (String.IsNullOrEmpty(uids))
            {
                await this.ExecuteAsync(
                    "delete from " + TableName + " where ListId = @listId;",
                    new { listId });
            }
            else
            {
                await this.ExecuteAsync(
                    "delete from " + TableName + " where " +
                    "ListId = @listId and UserId not in (" + uids + ");",
                    new { listId });
            }
        }
    }
}
