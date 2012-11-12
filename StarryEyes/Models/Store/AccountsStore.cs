using System;
using System.Collections.Generic;
using System.Linq;
using Livet;
using StarryEyes.Albireo.Data;
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

        private static AVLTree<long> _accountsIdCollection = null;

        /// <summary>
        /// 設定情報からアカウント情報を読み込み、アカウント情報が変化した際に自動で保存するトリガを準備します。
        /// </summary>
        internal static void Initialize()
        {
            if (isInitialized)
                throw new InvalidOperationException("AccountsModel has already initialized.");
            isInitialized = true;
            Setting._AccountsInternalDataStore.ForEach(Accounts.Add);
            // writeback delegate
            Accounts.CollectionChanged += (_, __) =>
            {
                Setting._AccountsInternalDataStore = Accounts;
                _accountsIdCollection = null;
            };
        }

        public static IReadOnlyCollection<long> AccountIds
        {
            get
            {
                return _accountsIdCollection ??
                    (_accountsIdCollection = new AVLTree<long>(Accounts.Select(_ => _.UserId)));
            }
        }

        public static AccountSetting GetAccountSetting(long id)
        {
            return accounts.Where(i => i.UserId == id).FirstOrDefault();
        }
    }
}