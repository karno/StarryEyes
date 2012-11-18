using System;
using System.Collections.Generic;
using System.Linq;
using Livet;
using StarryEyes.Models.Stores;
using StarryEyes.Breezy.Authorize;
using StarryEyes.Settings;

namespace StarryEyes.ViewModels.WindowParts
{
    /// <summary>
    /// アカウントを選択するコンポーネント用ビューモデル
    /// </summary>
    public class AccountSelectorViewModel : ViewModel
    {
        public AccountSelectorViewModel()
        {
            this.CompositeDisposable.Add(accounts = ViewModelHelper.CreateReadOnlyDispatcherCollection(
                AccountsStore.Accounts,
                _ => new SelectableAccountViewModel(_.AuthenticateInfo, RaiseSelectedAccountsChanged),
                DispatcherHelper.UIDispatcher));
        }

        private readonly ReadOnlyDispatcherCollection<SelectableAccountViewModel> accounts;
        public ReadOnlyDispatcherCollection<SelectableAccountViewModel> Accounts
        {
            get { return accounts; }
        }

        public void SetSelectedAccountIds(IEnumerable<long> accountIds)
        {
            var acl = accountIds.Guard().ToArray();
            Accounts.ForEach(i => i.IsSelected = acl.Contains(i.Id));
        }

        public IEnumerable<AuthenticateInfo> SelectedAccounts
        {
            get
            {
                return Accounts
                    .Where(i => i.IsSelected)
                    .Select(_ => _.AuthenticateInfo);
            }
            set
            {
                SetSelectedAccountIds(value.Select(i => i.Id));
            }
        }

        public event Action OnSelectedAccountsChanged;
        private void RaiseSelectedAccountsChanged()
        {
            var handler = OnSelectedAccountsChanged;
            if (handler != null)
                handler();
        }
    }

    /// <summary>
    /// AccountSelectorViewModelで選択されるアカウント
    /// </summary>
    public class SelectableAccountViewModel : ViewModel
    {
        private readonly AuthenticateInfo info;
        public AuthenticateInfo AuthenticateInfo
        {
            get { return info; }
        }

        private readonly Action onSelectionChanged;

        public SelectableAccountViewModel(AuthenticateInfo info, Action onSelectionChanged)
        {
            this.info = info;
            this.onSelectionChanged = onSelectionChanged;
        }

        private bool isSelected;
        public bool IsSelected
        {
            get { return isSelected; }
            set
            {
                if (isSelected != value)
                {
                    isSelected = value;
                    RaisePropertyChanged(() => IsSelected);
                    onSelectionChanged();
                }
            }
        }

        public long Id
        {
            get { return info.Id; }
        }

        public string ScreenName
        {
            get { return info.UnreliableScreenName; }
        }

        public Uri ProfileImageUri
        {
            get { return info.UnreliableProfileImageUri; }
        }
    }
}
