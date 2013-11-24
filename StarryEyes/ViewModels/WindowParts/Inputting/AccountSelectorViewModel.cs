using System;
using System.Linq;
using System.Reactive.Linq;
using Livet;
using StarryEyes.Models.Inputting;
using StarryEyes.Settings;
using StarryEyes.ViewModels.Common;
using StarryEyes.ViewModels.WindowParts.Flips;

namespace StarryEyes.ViewModels.WindowParts.Inputting
{
    public class AccountSelectorViewModel : ViewModel
    {
        private readonly InputViewModel _parent;
        private readonly AccountSelectionFlipViewModel _accountSelectionFlip;
        private readonly ReadOnlyDispatcherCollectionRx<TwitterAccountViewModel> _bindingAuthInfos;

        public AccountSelectorViewModel(InputViewModel parent)
        {
            _parent = parent;
            this._accountSelectionFlip = new AccountSelectionFlipViewModel();
            this.AccountSelectionFlip.Closed += () =>
            {
                // After selection accounts, return focus to text box
                // if input area is opened.
                if (_parent.IsOpening)
                {
                    _parent.FocusToTextBox();
                }
            };
            this.AccountSelectionFlip.SelectedAccountsChanged += () =>
            {
                InputModel.AccountSelector.Accounts.Clear();
                Setting.Accounts.Collection
                       .Where(a => AccountSelectionFlip.SelectedAccounts.Contains(a))
                       .ForEach(InputModel.AccountSelector.Accounts.Add);
            };
            CompositeDisposable.Add(this.AccountSelectionFlip);
            CompositeDisposable.Add(
                ViewModelHelperRx.CreateReadOnlyDispatcherCollectionRx(
                    InputModel.AccountSelector.Accounts,
                    a => new TwitterAccountViewModel(a),
                    DispatcherHelper.UIDispatcher));
            CompositeDisposable.Add(
                InputModel.AccountSelector.Accounts.ListenCollectionChanged()
                          .Subscribe(_ =>
                          {
                              RaisePropertyChanged(() => AuthInfoGridRowColumn);
                              this.RaisePropertyChanged(() => AuthInfoScreenNames);
                          }));
            CompositeDisposable.Add(_bindingAuthInfos =
                                    ViewModelHelperRx.CreateReadOnlyDispatcherCollectionRx(
                                        InputModel.AccountSelector.Accounts,
                                        account => new TwitterAccountViewModel(account),
                                        DispatcherHelper.UIDispatcher));
            CompositeDisposable.Add(_bindingAuthInfos
                                    .ListenCollectionChanged()
                                    .Subscribe(_ =>
                                    {
                                        this.RaisePropertyChanged(() => AuthInfoGridRowColumn);
                                        RaisePropertyChanged(() => IsBindingAuthInfoExisted);
                                    }));
        }

        public AccountSelectionFlipViewModel AccountSelectionFlip
        {
            get { return this._accountSelectionFlip; }
        }

        public ReadOnlyDispatcherCollectionRx<TwitterAccountViewModel> BindingAuthInfos
        {
            get { return this._bindingAuthInfos; }
        }

        public bool IsBindingAuthInfoExisted
        {
            get { return _bindingAuthInfos != null && _bindingAuthInfos.Count > 0; }
        }

        public int AuthInfoGridRowColumn
        {
            get { return (int)Math.Ceiling(Math.Sqrt(Math.Max(_bindingAuthInfos.Count, 1))); }
        }

        public string AuthInfoScreenNames
        {
            get
            {
                if (_bindingAuthInfos.Count == 0)
                    return "アカウントは選択されていません。";
                return _bindingAuthInfos.Select(_ => _.ScreenName).JoinString(", ") + "が選択されています。";
            }
        }
    }
}
