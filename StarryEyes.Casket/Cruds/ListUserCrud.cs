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
            await base.InitializeAsync(descriptor);
            await this.CreateIndexAsync("ListUserLID", "ListId", false);
            await this.CreateIndexAsync("ListUserUID", "UserId", false);
        }

        public async Task<IEnumerable<long>> GetUsersAsync(long listId)
        {
            return (await Descriptor.QueryAsync<DatabaseListUser>(
                CreateSql("ListId = @listId"), new { listId }))
                .Select(l => l.UserId);
        }

        public async Task RegisterUsersAsync(long listId, IEnumerable<long> userIds)
        {
            await Descriptor.ExecuteAllAsync(userIds.Select(
                uid => Tuple.Create(this.TableInserter, (object)new DatabaseListUser(listId, uid))));
        }

        public async Task RegisterUserAsync(long listId, long userId)
        {
            await InsertAsync(new DatabaseListUser(listId, userId));
        }

        public async Task DeleteUsersAsync(long listId, IEnumerable<long> removalUserIds)
        {
            var uids = removalUserIds.Select(i => i.ToString(CultureInfo.InvariantCulture)).JoinString(",");
            if (String.IsNullOrEmpty(uids)) return;
            await Descriptor.ExecuteAsync(
                string.Format("delete from {0} where " + "ListId = @listId and UserId in ({1});", this.TableName, uids),
                new { listId });
        }

        public async Task DeleteUserAsync(long listId, long userId)
        {
            await Descriptor.ExecuteAsync(
                string.Format("delete from {0} where " + "ListId = @listId and UserId = @userId", this.TableName),
                new { listId, userId });
        }

        public async Task RemoveUnspecifiedUsersAsync(long listId, IEnumerable<long> users)
        {
            var uids = users.Select(i => i.ToString(CultureInfo.InvariantCulture)).JoinString(",");
            if (String.IsNullOrEmpty(uids))
            {
                await Descriptor.ExecuteAsync(
                    string.Format("delete from {0} where ListId = @listId;", this.TableName),
                    new { listId });
            }
            else
            {
                await Descriptor.ExecuteAsync(
                    string.Format("delete from {0} where " + "ListId = @listId and UserId not in ({1});",
                        this.TableName, uids),
                    new { listId });
            }
        }
    }
}
