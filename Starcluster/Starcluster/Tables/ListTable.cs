using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Starcluster.Connections;
using Starcluster.Infrastructures;
using Starcluster.Models;

namespace Starcluster.Tables
{
    public class ListTable : TableBase<DbTwitterList>
    {
        public ListTable(string tableName) : base(tableName, ResolutionMode.Replace)
        {
        }

        public override async Task InitializeAsync(IDatabaseConnectionDescriptor descriptor)
        {
            await base.InitializeAsync(descriptor).ConfigureAwait(false);
            await CreateIndexAsync("UID", nameof(DbTwitterList.UserId), false).ConfigureAwait(false);
            await CreateIndexAsync("SLUG", nameof(DbTwitterList.Slug), false).ConfigureAwait(false);
        }

        public Task RegisterListAsync(DbTwitterList list)
        {
            return InsertAsync(list);
        }

        public async Task<DbTwitterList> GetAsync(long userId, string slug)
        {
            return (await Descriptor.QueryAsync<DbTwitterList>(
                CreateSql($"{nameof(DbTwitterList.UserId)} = @userId AND " +
                          $"LOWER({nameof(DbTwitterList.Slug)}) = LOWER(@slug) LIMIT 1"),
                new { userId, slug }).ConfigureAwait(false)).FirstOrDefault();
        }

        public Task<IEnumerable<DbTwitterList>> FindOwnedListAsync(long userId)
        {
            return Descriptor.QueryAsync<DbTwitterList>(
                CreateSql(nameof(DbTwitterList.UserId) + " = @userId"),
                new { userId });
        }
    }
}