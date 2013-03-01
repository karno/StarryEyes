using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive.Linq;
using Livet;
using StarryEyes.Breezy.Api.Rest;
using StarryEyes.Breezy.Authorize;
using StarryEyes.Models;
using StarryEyes.Models.Backpanels.SystemEvents;
using StarryEyes.Models.Stores;
using StarryEyes.Settings;

namespace StarryEyes.ViewModels.WindowParts.Flips.SearchFlip
{
    public class SearchCandidateViewModel : ViewModel
    {
        public SearchCandidateViewModel()
        {
            this.CompositeDisposable.Add(
                _accounts = ViewModelHelperRx.CreateReadOnlyDispatcherCollectionRx(
                AccountsStore.Accounts,
                a => new AccountBoundSearchCandidateViewModel(a.AuthenticateInfo),
                DispatcherHolder.Dispatcher));
        }

        private readonly ReadOnlyDispatcherCollectionRx<AccountBoundSearchCandidateViewModel> _accounts;
        public ReadOnlyDispatcherCollectionRx<AccountBoundSearchCandidateViewModel> Accounts
        {
            get { return _accounts; }
        }
    }

    public class AccountBoundSearchCandidateViewModel : ViewModel
    {
        private readonly AuthenticateInfo _auth;
        public AuthenticateInfo AuthenticateInfo
        {
            get { return _auth; }
        }

        public AccountBoundSearchCandidateViewModel(AuthenticateInfo auth)
        {
            _auth = auth;
        }

        public void LoadSearches()
        {
            var ccoc = _candidates = new ObservableCollection<SearchCandidateItemViewModel>();
            _auth.GetSavedSearchs()
                .Finally(() => IsLoading = false)
                .ObserveOnDispatcher()
                .Subscribe(
                    j => ccoc.Add(new SearchCandidateItemViewModel(j.id, j.query, () => Remove(j.id))),
                    ex => IsAvailable = false);
            RaisePropertyChanged(() => Candidates);
        }

        private void Remove(long id)
        {
            _auth.DestroySavedSearch(id)
                 .Subscribe(_ =>
                 {
                     var idx = _candidates.TakeWhile(vm => vm.Id == id).Count();
                     if (idx < _candidates.Count)
                     {
                         _candidates.RemoveAt(idx);
                     }
                 }, ex => BackpanelModel.RegisterEvent(
                     new OperationFailedEvent(ex.Message)));
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get { return _isLoading; }
            set
            {
                _isLoading = value;
                RaisePropertyChanged();
            }
        }

        private bool _isAvailable;

        public bool IsAvailable
        {
            get { return _isAvailable; }
            set
            {
                _isAvailable = value;
                RaisePropertyChanged();
            }
        }

        private ObservableCollection<SearchCandidateItemViewModel> _candidates =
            new ObservableCollection<SearchCandidateItemViewModel>();
        public ObservableCollection<SearchCandidateItemViewModel> Candidates
        {
            get { return _candidates; }
        }
    }

    public class SearchCandidateItemViewModel : ViewModel
    {
        private readonly long _id;
        private readonly string _query;
        private readonly Action _onRemove;

        public SearchCandidateItemViewModel(long id, string query, Action onRemove)
        {
            _id = id;
            _query = query;
            _onRemove = onRemove;
        }

        public string Query
        {
            get { return _query; }
        }

        public long Id
        {
            get { return _id; }
        }

        public void Exec()
        {

        }

        public void Remove()
        {
            _onRemove();
        }
    }
}
