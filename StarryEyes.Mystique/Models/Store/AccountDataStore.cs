using System.Collections.Generic;
using StarryEyes.Mystique.Models.Common;
using StarryEyes.SweetLady.Authorize;

namespace StarryEyes.Mystique.Models.Store
{
    public static class AccountDataStore
    {
        private static object storeLocker = new object();
        private static SortedDictionary<long, AccountData> store;

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

        public static AccountData GetAccountData(this AuthenticateInfo info)
        {
            return GetAccountData(info.Id);
        }
    }
}
