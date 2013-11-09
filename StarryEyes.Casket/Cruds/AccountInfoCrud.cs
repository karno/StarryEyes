using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StarryEyes.Casket.Cruds.Scaffolding;
using StarryEyes.Casket.DatabaseModels;

namespace StarryEyes.Casket.Cruds
{
    public class AccountInfoCrud : CrudBase<DatabaseAccountInfo>
    {
        public AccountInfoCrud()
            : base(ResolutionMode.Ignore)
        {
        }

        public async Task<IEnumerable<long>> GetAllAsync()
        {
            return (await this.QueryAsync<DatabaseAccountInfo>("select * from " + TableName + ";", null))
                .Select(a => a.Id);
        }

        public async Task DropAllAsync()
        {
            await this.ExecuteAsync("delete from " + TableName + ";");
        }
    }
}
