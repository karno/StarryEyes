using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StarryEyes.Casket.Connections;
using StarryEyes.Casket.Cruds.Scaffolding;
using StarryEyes.Casket.DatabaseModels;

namespace StarryEyes.Casket.Cruds
{
    public abstract class ActivityCrudBase : CrudBase<DatabaseActivity>
    {
        private readonly string _tableName;

        protected ActivityCrudBase(string tableName)
            : base(tableName, ResolutionMode.Ignore)
        {
            _tableName = tableName;
        }

        internal override async Task InitializeAsync(IDatabaseConnectionDescriptor descriptor)
        {
            await base.InitializeAsync(descriptor).ConfigureAwait(false);
            await CreateIndexAsync(_tableName + "_SID", "StatusId", false).ConfigureAwait(false);
            await CreateIndexAsync(_tableName + "_UID", "UserId", false).ConfigureAwait(false);
        }

        public Task<IEnumerable<long>> GetUsersAsync(long statusId)
        {
            return Descriptor.QueryAsync<long>(
                "select UserId from " + TableName + " where StatusId = @Id;",
                new { Id = statusId });
        }

        public async Task<Dictionary<long, IEnumerable<long>>> GetUsersDictionaryAsync(
            IEnumerable<long> statusIds)
        {
            return (await statusIds
                    .Chunk(Database.MAX_PARAM_LENGTH)
                    .Select(chunk => Descriptor.QueryAsync<DatabaseActivity>(
                        CreateSql("StatusId IN @Ids"), new { Ids = chunk.ToArray() }))
                    .GatherSelectMany().ConfigureAwait(false))
                .GroupBy(e => e.StatusId)
                .ToDictionary(e => e.Key, e => e.Select(d => d.UserId));
        }

        public Task DeleteNotExistsAsync(string statusTableName)
        {
            return Descriptor.ExecuteAsync(
                string.Format("DELETE FROM {0} WHERE NOT EXISTS (SELECT Id FROM {1} WHERE {1}.Id = {0}.StatusId);",
                    TableName, statusTableName));
        }

        public Task InsertAsync(long statusId, long userId)
        {
            return base.InsertAsync(new DatabaseActivity
            {
                StatusId = statusId,
                UserId = userId
            });
        }

        public Task DeleteAsync(long statusId, long userId)
        {
            return Descriptor.ExecuteAsync(
                string.Format("delete from {0} where StatusId = @Sid and UserId = @Uid;", TableName),
                new { Sid = statusId, Uid = userId });
        }

        public Task InsertAllAsync(IEnumerable<Tuple<long, long>> items)
        {
            var inserters = items.Select(i => Tuple.Create(
                TableInserter,
                (object)new DatabaseActivity
                {
                    StatusId = i.Item1,
                    UserId = i.Item2
                }));
            return Descriptor.ExecuteAllAsync(inserters);
        }

        public Task DeleteAllAsync(IEnumerable<Tuple<long, long>> items)
        {
            var deleters = items.Select(i => Tuple.Create(
                "delete from " + TableName + " where StatusId = @Sid and UserId = @Uid;",
                (object)new { Sid = i.Item1, Uid = i.Item2 }));
            return Descriptor.ExecuteAllAsync(deleters);
        }
    }
}