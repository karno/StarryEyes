using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using StarryEyes.Casket.Connections;
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

        internal override async Task InitializeAsync(IDatabaseConnectionDescriptor descriptor)
        {
            await base.InitializeAsync(descriptor).ConfigureAwait(false);
            await CreateIndexAsync("ListUserLID", "ListId", false).ConfigureAwait(false);
            await CreateIndexAsync("ListUserUID", "UserId", false).ConfigureAwait(false);
        }

        public async Task<IEnumerable<long>> GetUsersAsync(long listId)
        {
            return (await Descriptor.QueryAsync<DatabaseListUser>(
                CreateSql("ListId = @listId"), new { listId }).ConfigureAwait(false))
                .Select(l => l.UserId);
        }

        public Task RegisterUsersAsync(long listId, IEnumerable<long> userIds)
        {
            return Descriptor.ExecuteAllAsync(userIds.Select(
                uid => Tuple.Create(TableInserter, (object)new DatabaseListUser(listId, uid))));
        }

        public Task RegisterUserAsync(long listId, long userId)
        {
            return InsertAsync(new DatabaseListUser(listId, userId));
        }

        public Task DeleteUsersAsync(long listId, IEnumerable<long> removalUserIds)
        {
            var uids = removalUserIds.Select(i => i.ToString(CultureInfo.InvariantCulture)).JoinString(",");
            if (String.IsNullOrEmpty(uids)) return Task.Run(() => { });
            return Descriptor.ExecuteAsync(
                string.Format("delete from {0} where " + "ListId = @listId and UserId in ({1});", TableName, uids),
                new { listId });
        }

        public Task DeleteUserAsync(long listId, long userId)
        {
            return Descriptor.ExecuteAsync(
                string.Format("delete from {0} where " + "ListId = @listId and UserId = @userId", TableName),
                new { listId, userId });
        }

        public Task RemoveUnspecifiedUsersAsync(long listId, IEnumerable<long> users)
        {
            var uids = users.Select(i => i.ToString(CultureInfo.InvariantCulture)).JoinString(",");
            if (String.IsNullOrEmpty(uids))
            {
                return Descriptor.ExecuteAsync(
                    string.Format("delete from {0} where ListId = @listId;", TableName),
                    new { listId });
            }
            else
            {
                return Descriptor.ExecuteAsync(
                    string.Format("delete from {0} where " + "ListId = @listId and UserId not in ({1});",
                        TableName, uids),
                    new { listId });
            }
        }
    }
}
