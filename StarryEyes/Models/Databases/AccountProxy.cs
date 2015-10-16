using System.Collections.Generic;
using System.Threading.Tasks;
using StarryEyes.Casket;
using StarryEyes.Casket.DatabaseModels;

namespace StarryEyes.Models.Databases
{
    internal static class AccountProxy
    {
        public static async Task<IEnumerable<long>> GetAccountsAsync()
        {
            return await Database.AccountInfoCrud.GetAllAsync().ConfigureAwait(false);
        }

        public static async Task AddAccountAsync(long id)
        {
            await Database.AccountInfoCrud.InsertAsync(new DatabaseAccountInfo(id)).ConfigureAwait(false);
        }

        public static async Task RemoveAccountAsync(long id)
        {
            await Database.AccountInfoCrud.DeleteAsync(id).ConfigureAwait(false);
            await Database.RelationCrud.DropUserAsync(id).ConfigureAwait(false);
        }

        public static async Task RemoveAllAccountsAsync()
        {
            await Database.AccountInfoCrud.DropAllAsync().ConfigureAwait(false);
            await Database.RelationCrud.DropAllAsync().ConfigureAwait(false);
        }
    }
}
