using System.Collections.Generic;
using System.Linq;
using StarryEyes.Mystique.Models.Common;
using StarryEyes.SweetLady.Authorize;

namespace StarryEyes.Mystique.Models.Store
{
    /// <summary>
    /// Account data store
    /// </summary>
    public static class AccountDataStore
    {
        private static object storeLocker = new object();
        private static SortedDictionary<long, AccountData> store = new SortedDictionary<long, AccountData>();

        /// <summary>
        /// Get account data fot the account id.<para />
        /// If not exited, create it new.
        /// </summary>
        /// <param name="id">account id</param>
        /// <returns>account data</returns>
        public static AccountData GetAccountData(long id)
        {
            AccountData data;
            lock (storeLocker)
            {
                if (store.TryGetValue(id, out data))
                    return data;
                data = new AccountData(id);
                store.Add(id, data);
                return data;
            }
        }

        /// <summary>
        /// Get account data for the account.
        /// </summary>
        /// <param name="info">lookup account</param>
        /// <returns>account data</returns>
        public static AccountData GetAccountData(this AuthenticateInfo info)
        {
            return GetAccountData(info.Id);
        }

        /// <summary>
        /// Store data for an account.
        /// </summary>
        /// <param name="data">storing data</param>
        public static void SetAccountData(AccountData data)
        {
            lock (storeLocker)
            {
                store[data.AccountId] = data;
            }
        }

        /// <summary>
        /// Get all existed datas.
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<AccountData> GetAccountDatas()
        {
            lock (storeLocker)
            {
                return store.Values.ToArray();
            }
        }
    }
}
