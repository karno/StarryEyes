using System;
using System.Linq;
using Livet;
using StarryEyes.Settings;

namespace StarryEyes.Models.Store
{
    public static class AccountsStore
    {
        private static bool isInitialized = false;

        private readonly static ObservableSynchronizedCollection<AccountSetting> accounts =
            new ObservableSynchronizedCollection<AccountSetting>();
        public static ObservableSynchronizedCollection<AccountSetting> Accounts
        {
            get { return AccountsStore.accounts; }
        }

        /// <summary>
        /// 設定情報からアカウント情報を読み込み、アカウント情報が変化した際に自動で保存するトリガを準備します。
        /// </summary>
        internal static void Initialize()
        {
            if (isInitialized)
                throw new InvalidOperationException("AccountsModel has already initialized.");
            isInitialized = true;
            Setting.Accounts.ForEach(a => Accounts.Add(a));
            // writeback delegate
            Accounts.CollectionChanged += (_, __) => Setting.Accounts = Accounts;
        }

        public static AccountSetting GetAccountSetting(long id)
        {
            return accounts.Where(i => i.UserId == id).FirstOrDefault();
        }
    }
}
