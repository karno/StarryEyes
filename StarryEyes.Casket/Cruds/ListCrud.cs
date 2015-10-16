using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StarryEyes.Casket.Connections;
using StarryEyes.Casket.Cruds.Scaffolding;
using StarryEyes.Casket.DatabaseModels;

namespace StarryEyes.Casket.Cruds
{
    public class ListCrud : CrudBase<DatabaseList>
    {
        public ListCrud()
            : base(ResolutionMode.Replace)
        {
        }

        internal override async Task InitializeAsync(IDatabaseConnectionDescriptor descriptor)
        {
            await base.InitializeAsync(descriptor).ConfigureAwait(false);
            await CreateIndexAsync("ListFName", "FullName", false).ConfigureAwait(false);
            await CreateIndexAsync("ListUID", "UserId", false).ConfigureAwait(false);
            await CreateIndexAsync("ListSlug", "Slug", false).ConfigureAwait(false);
        }

        public Task RegisterListAsync(DatabaseList list)
        {
            return InsertAsync(list);
        }

        public async Task<DatabaseList> GetAsync(long userId, string slug)
        {
            return (await Descriptor.QueryAsync<DatabaseList>(
                CreateSql("UserId = @userId and LOWER(Slug) = LOWER(@slug) limit 1"),
                new { userId, slug }).ConfigureAwait(false)).FirstOrDefault();
        }

        public Task<IEnumerable<DatabaseList>> FindOwnedListAsync(long userId)
        {
            return Descriptor.QueryAsync<DatabaseList>(
                CreateSql("UserId = @userId"),
                new { userId });
        }
    }
}
