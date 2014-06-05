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
            return await Database.AccountInfoCrud.GetAllAsync();
        }

        public static async Task AddAccountAsync(long id)
        {
            await Database.AccountInfoCrud.InsertAsync(new DatabaseAccountInfo(id));
        }

        public static async Task RemoveAccountAsync(long id)
        {
            await Database.AccountInfoCrud.DeleteAsync(id);
            await Database.RelationCrud.DropUserAsync(id);
        }

        public static async Task RemoveAllAccountsAsync()
        {
            await Database.AccountInfoCrud.DropAllAsync();
            await Database.RelationCrud.DropAllAsync();
        }
    }
}
