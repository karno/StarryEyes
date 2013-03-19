using System;
using System.Collections.Generic;
using System.Linq;
using Livet;
using StarryEyes.Albireo.Data;
using StarryEyes.Settings;

namespace StarryEyes.Models.Stores
{
    public static class AccountsStore
    {
        private static bool _isInitialized;

        private readonly static ObservableSynchronizedCollection<AccountSetting> _accounts =
            new ObservableSynchronizedCollection<AccountSetting>();
        public static ObservableSynchronizedCollection<AccountSetting> Accounts
        {
            get { return _accounts; }
        }

        private static AVLTree<long> _accountsIdCollection;

        /// <summary>
        /// 設定情報からアカウント情報を読み込み、アカウント情報が変化した際に自動で保存するトリガを準備します。
        /// </summary>
        internal static void Initialize()
        {
            if (_isInitialized)
                throw new InvalidOperationException("AccountsModel has already initialized.");
            _isInitialized = true;
            Setting.Infrastructure_Accounts.ForEach(Accounts.Add);
            // writeback delegate
            Accounts.CollectionChanged += (_, __) =>
            {
                Setting.Infrastructure_Accounts = Accounts;
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
            return _accounts.FirstOrDefault(i => i.UserId == id);
        }
    }
}