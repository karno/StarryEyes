using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Livet;
using StarryEyes.Breezy.Api.Rest;
using StarryEyes.Breezy.Authorize;
using StarryEyes.Models.Stores;
using StarryEyes.Views.Messaging;

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

        private string _selectionReason;
        /// <summary>
        /// Reason of selecting account
        /// </summary>
        public string SelectionReason
        {
            get { return _selectionReason; }
            set
            {
                _selectionReason = value;
                RaisePropertyChanged(() => SelectionReason);
            }
        }

        private ReadOnlyDispatcherCollection<SelectableAccountViewModel> accounts;
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

        private bool _isVisible = false;
        public bool IsVisible
        {
            get { return _isVisible; }
            set
            {
                _isVisible = value;
                RaisePropertyChanged(() => IsVisible);
            }
        }

        public void Open()
        {
            this.Messenger.Raise(new GoToStateMessage("Open"));
        }

        public void Close()
        {
            this.Messenger.Raise(new GoToStateMessage("Close"));
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
            get
            {
                if (info.UnreliableProfileImageUri == null)
                {
                    Task.Run(() => info.ShowUser(info.Id)
                        .Subscribe(_ =>
                        {
                            info.UnreliableProfileImageUriString = _.ProfileImageUri.OriginalString;
                            RaisePropertyChanged(() => ProfileImageUri);
                        }));
                }
                return info.UnreliableProfileImageUri;
            }
        }
    }
}
