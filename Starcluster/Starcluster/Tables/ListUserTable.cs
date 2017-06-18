using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Starcluster.Connections;
using Starcluster.Infrastructures;
using Starcluster.Models;

namespace Starcluster.Tables
{
    public class ListUserTable : TableBase<DbTwitterListUser>
    {
        public ListUserTable(string tableName) : base(tableName, ResolutionMode.Ignore)
        {
        }

        public override async Task InitializeAsync(IDatabaseConnectionDescriptor descriptor)
        {
            await base.InitializeAsync(descriptor).ConfigureAwait(false);
            await CreateIndexAsync("LID", nameof(DbTwitterListUser.ListId), false).ConfigureAwait(false);
            await CreateIndexAsync("UID", nameof(DbTwitterListUser.UserId), false).ConfigureAwait(false);
        }

        public async Task<IEnumerable<long>> GetUsersAsync(long listId)
        {
            return (await Descriptor.QueryAsync<DbTwitterListUser>(
                                        CreateSql(nameof(DbTwitterListUser.ListId) + " = @listId"), new { listId })
                                    .ConfigureAwait(false))
                .Select(l => l.UserId);
        }

        // add all users and remove unspecified users
        public Task SetUsersAsync(long listId, IEnumerable<long> userIds)
        {
            var delQuery = Tuple.Create($"DELETE FROM {TableName} WHERE {nameof(DbTwitterListUser.ListId)} = @listId;",
                (object)new { listId });
            return Descriptor.ExecuteAllAsync(
                new[] { delQuery }.Concat(userIds.Select(
                    uid => Tuple.Create(TableInserter, (object)new DbTwitterListUser(listId, uid)))));
        }

        public Task RegisterUsersAsync(long listId, IEnumerable<long> userIds)
        {
            return Descriptor.ExecuteAllAsync(userIds.Select(
                uid => Tuple.Create(TableInserter, (object)new DbTwitterListUser(listId, uid))));
        }

        public Task RegisterUserAsync(long listId, long userId)
        {
            return InsertAsync(new DbTwitterListUser(listId, userId));
        }

        public Task DeleteUsersAsync(long listId, IEnumerable<long> removalUserIds)
        {
            var uids = String.Join(", ", removalUserIds.Select(i => i.ToString(CultureInfo.InvariantCulture)));
            if (String.IsNullOrEmpty(uids)) return Task.Run(() => { });
            return Descriptor.ExecuteAsync(
                $"DELETE FROM {TableName} WHERE {nameof(DbTwitterListUser.ListId)} = @listId AND UserId IN ({uids});",
                new { listId });
        }

        public Task DeleteUsersAsync(long listId)
        {
            return Descriptor.ExecuteAsync(
                $"DELETE FROM {TableName} WHERE {nameof(DbTwitterListUser.ListId)} = @listId;",
                new { listId });
        }

        public Task DeleteUserAsync(long listId, long userId)
        {
            return Descriptor.ExecuteAsync(
                $"DELETE FROM {TableName} WHERE {nameof(DbTwitterListUser.ListId)} = @listId AND UserId = @userId",
                new { listId, userId });
        }

        public Task RemoveUnspecifiedUsersAsync(long listId, IEnumerable<long> users)
        {
            var uids = String.Join(",", users.Select(i => i.ToString(CultureInfo.InvariantCulture)));
            return
                Descriptor.ExecuteAsync(
                    String.IsNullOrEmpty(uids)
                        ? $"DELETE FROM {TableName} WHERE {nameof(DbTwitterListUser.ListId)} = @listId;"
                        : $"DELETE FROM {TableName} WHERE {nameof(DbTwitterListUser.ListId)} = @listId AND UserId not in ({uids});",
                    new { listId });
        }
    }
}