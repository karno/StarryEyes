using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Livet;
using StarryEyes.Albireo;
using StarryEyes.Albireo.Helpers;
using StarryEyes.Anomaly.TwitterApi.Rest;
using StarryEyes.Anomaly.Utils;
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
            this.CompositeDisposable.Add(_accounts = ViewModelHelperRx.CreateReadOnlyDispatcherCollectionRx(
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
            get { return _selectionReason; }
            set
            {
                _selectionReason = value;
                RaisePropertyChanged(() => SelectionReason);
            }
        }

        private readonly ReadOnlyDispatcherCollectionRx<SelectableAccountViewModel> _accounts;
        public ReadOnlyDispatcherCollectionRx<SelectableAccountViewModel> Accounts
        {
            get { return _accounts; }
        }

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
            set
            {
                SetSelectedAccountIds(value.Guard().Select(i => i.Id));
            }
        }

        public event Action SelectedAccountsChanged;
        private void RaiseSelectedAccountsChanged()
        {
            this.SelectedAccountsChanged.SafeInvoke();
        }
    }

    /// <summary>
    /// Represents account in the AccountSelectorViewModel.
    /// </summary>
    public class SelectableAccountViewModel : ViewModel
    {
        private readonly TwitterAccount _account;
        public TwitterAccount TwitterAccount
        {
            get { return this._account; }
        }

        private readonly AccountSelectionFlipViewModel _parent;

        private readonly Action _onSelectionChanged;

        public SelectableAccountViewModel(AccountSelectionFlipViewModel parent, TwitterAccount account, Action onSelectionChanged)
        {
            this._parent = parent;
            this._account = account;
            this._onSelectionChanged = onSelectionChanged;
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (this._isSelected == value) return;
                this._isSelected = value;
                this.RaisePropertyChanged(() => this.IsSelected);
                this._onSelectionChanged();
            }
        }

        public long Id
        {
            get { return this._account.Id; }
        }

        public string ScreenName
        {
            get { return this._account.UnreliableScreenName; }
        }

        public Uri ProfileImageUri
        {
            get
            {
                if (this._account.UnreliableProfileImage == null)
                {
                    Task.Run(async () =>
                    {
                        try
                        {
                            var user = await this._account.ShowUserAsync(this._account.Id);
                            this._account.UnreliableProfileImage = user.ProfileImageUri.ChangeImageSize(ImageSize.Original);
                            this.RaisePropertyChanged(() => ProfileImageUri);
                        }
                        catch { }
                    });
                }
                return this._account.UnreliableProfileImage;
            }
        }

        public void ToggleSelection()
        {
            this.IsSelected = !IsSelected;
        }

        public void SelectExcepted()
        {
            _parent.SelectedAccounts = new[] { this._account };
        }
    }
}
