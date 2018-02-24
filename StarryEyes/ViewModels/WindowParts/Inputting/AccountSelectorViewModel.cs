using System;
using System.Linq;
using JetBrains.Annotations;
using Livet;
using StarryEyes.Albireo.Helpers;
using StarryEyes.Globalization;
using StarryEyes.Globalization.WindowParts;
using StarryEyes.Models.Accounting;
using StarryEyes.Models.Inputting;
using StarryEyes.Settings;
using StarryEyes.ViewModels.Common;
using StarryEyes.ViewModels.WindowParts.Flips;

namespace StarryEyes.ViewModels.WindowParts.Inputting
{
    public class AccountSelectorViewModel : ViewModel
    {
        private readonly InputViewModel _parent;
        private readonly ReadOnlyDispatcherCollectionRx<TwitterAccountViewModel> _accounts;

        public AccountSelectorViewModel(InputViewModel parent)
        {
            _parent = parent;
            AccountSelectionFlip = new AccountSelectionFlipViewModel();
            AccountSelectionFlip.Closed += () =>
            {
                // After selection accounts, return focus to text box
                // if input area is opened.
                if (_parent.IsOpening)
                {
                    _parent.FocusToTextBox();
                }
            };
            AccountSelectionFlip.SelectedAccountsChanged += () =>
            {
                InputModel.AccountSelector.Accounts.Clear();
                Setting.Accounts.Collection
                       .Where(a => AccountSelectionFlip.SelectedAccounts.Contains(a))
                       .ForEach(InputModel.AccountSelector.Accounts.Add);
            };
            CompositeDisposable.Add(AccountSelectionFlip);
            CompositeDisposable.Add(_accounts =
                ViewModelHelperRx.CreateReadOnlyDispatcherCollectionRx(
                    InputModel.AccountSelector.Accounts,
                    a => new TwitterAccountViewModel(a),
                    DispatcherHelper.UIDispatcher));
            CompositeDisposable.Add(_accounts.ListenCollectionChanged(_ =>
            {
                RaisePropertyChanged(() => AuthInfoGridRowColumn);
                RaisePropertyChanged(() => AuthInfoScreenNames);
                RaisePropertyChanged(() => IsBoundAccountExists);
            }));
            CompositeDisposable.Add(
                InputModel.AccountSelector.ListenPropertyChanged(
                    () => InputModel.AccountSelector.IsSynchronizedWithTab,
                    _ => RaisePropertyChanged(() => IsSynchronizedWithTab)));
        }

        public AccountSelectionFlipViewModel AccountSelectionFlip { get; }

        public ReadOnlyDispatcherCollectionRx<TwitterAccountViewModel> Accounts => _accounts;

        public bool IsBoundAccountExists => _accounts != null && _accounts.Count > 0;

        public int AuthInfoGridRowColumn => (int)Math.Ceiling(Math.Sqrt(Math.Max(_accounts.Count, 1)));

        public string AuthInfoScreenNames
        {
            get
            {
                return _accounts.Count == 0
                    ? InputAreaResources.AccountSelectorNotSelected
                    : InputAreaResources.AccountSelectorSelectedFormat.SafeFormat(
                        _accounts.Select(_ => _.ScreenName).JoinString(", "));
            }
        }

        public bool IsSynchronizedWithTab => InputModel.AccountSelector.IsSynchronizedWithTab;

        [UsedImplicitly]
        public void SynchronizeWithTab()
        {
            InputModel.AccountSelector.SynchronizeWithTab();
        }

        [UsedImplicitly]
        public void SelectAccounts()
        {
            // synchronize accounts
            AccountSelectionFlip.SelectedAccounts = InputModel.AccountSelector.Accounts;
            AccountSelectionFlip.Open();
        }

        public void SelectNext()
        {
            var next = GetNextOrPreviousAccount(true);
            InputModel.AccountSelector.Accounts.Clear();
            InputModel.AccountSelector.Accounts.Add(next);
        }

        public void SelectPrev()
        {
            var prev = GetNextOrPreviousAccount(false);
            InputModel.AccountSelector.Accounts.Clear();
            InputModel.AccountSelector.Accounts.Add(prev);
        }

        private TwitterAccount GetNextOrPreviousAccount(bool next)
        {
            var account = Setting.Accounts.Collection.ToArray();
            if (account.Length < 1)
            {
                // accounts are not registered yet.
                return null;
            }
            int index;
            if (next)
            {
                index = Array.IndexOf(account, InputModel.AccountSelector.Accounts.LastOrDefault()) + 1;
                if (index >= account.Length)
                {
                    index = 0;
                }
            }
            else
            {
                index = Array.IndexOf(account, InputModel.AccountSelector.Accounts.FirstOrDefault()) - 1;
                if (index < 0)
                {
                    index = account.Length - 1;
                }
            }
            return account[index];
        }

        public void ClearAll()
        {
            InputModel.AccountSelector.Accounts.Clear();
        }

        public void SelectAll()
        {
            InputModel.AccountSelector.Accounts.Clear();
            Setting.Accounts.Collection.ForEach(InputModel.AccountSelector.Accounts.Add);
        }
    }
}