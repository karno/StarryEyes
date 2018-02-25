using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cadena.Api.Parameters;
using Cadena.Api.Rest;
using Cadena.Util;
using Livet;
using StarryEyes.Models.Accounting;
using StarryEyes.Settings;

namespace StarryEyes.ViewModels.WindowParts.Flips
{
    /// <summary>
    /// ViewModel for account selector component
    /// </summary>
    public class AccountSelectionFlipViewModel : PartialFlipViewModelBase
    {
        public AccountSelectionFlipViewModel()
        {
            CompositeDisposable.Add(Accounts = ViewModelHelperRx.CreateReadOnlyDispatcherCollectionRx(
                Setting.Accounts.Collection,
                account => new SelectableAccountViewModel(this, account, RaiseSelectedAccountsChanged),
                DispatcherHelper.UIDispatcher));
        }

        private string _selectionReason;

        /// <summary>
        /// Reason of selecting account
        /// </summary>
        public string SelectionReason
        {
            get => _selectionReason;
            set
            {
                _selectionReason = value;
                RaisePropertyChanged(() => SelectionReason);
            }
        }

        public ReadOnlyDispatcherCollectionRx<SelectableAccountViewModel> Accounts { get; }

        public void SetSelectedAccountIds(IEnumerable<long> accountIds)
        {
            var acl = accountIds.Guard().ToArray();
            Accounts.ForEach(i => i.IsSelected = acl.Contains(i.Id));
        }

        public IEnumerable<TwitterAccount> SelectedAccounts
        {
            get
            {
                return Accounts
                    .Where(i => i.IsSelected)
                    .Select(_ => _.TwitterAccount);
            }
            set { SetSelectedAccountIds(value.Guard().Select(i => i.Id)); }
        }

        public event Action SelectedAccountsChanged;

        private void RaiseSelectedAccountsChanged()
        {
            SelectedAccountsChanged?.Invoke();
        }
    }

    /// <summary>
    /// Represents account in the AccountSelectorViewModel.
    /// </summary>
    public class SelectableAccountViewModel : ViewModel
    {
        private readonly TwitterAccount _account;

        public TwitterAccount TwitterAccount => _account;

        private readonly AccountSelectionFlipViewModel _parent;

        private readonly Action _onSelectionChanged;

        public SelectableAccountViewModel(AccountSelectionFlipViewModel parent, TwitterAccount account,
            Action onSelectionChanged)
        {
            _parent = parent;
            _account = account;
            _onSelectionChanged = onSelectionChanged;
        }

        private bool _isSelected;

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value) return;
                _isSelected = value;
                RaisePropertyChanged(() => IsSelected);
                _onSelectionChanged();
            }
        }

        public long Id => _account.Id;

        public string ScreenName => _account.UnreliableScreenName;

        public Uri ProfileImageUri
        {
            get
            {
                if (_account.UnreliableProfileImage == null)
                {
                    Task.Run(async () =>
                    {
                        try
                        {
                            var user = await _account.CreateAccessor().ShowUserAsync(
                                new UserParameter(_account.Id), CancellationToken.None);
                            _account.UnreliableProfileImage =
                                user.Result.ProfileImageUri.ChangeImageSize(ImageSize.Original);
                            RaisePropertyChanged(() => ProfileImageUri);
                        }
                        catch
                        {
                            // ignored
                        }
                    });
                }
                return _account.UnreliableProfileImage;
            }
        }

        public void ToggleSelection()
        {
            IsSelected = !IsSelected;
        }

        public void SelectExcepted()
        {
            _parent.SelectedAccounts = new[] { _account };
        }
    }
}