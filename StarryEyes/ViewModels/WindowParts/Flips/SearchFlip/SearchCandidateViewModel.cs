using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using Livet;
using StarryEyes.Breezy.Api.Rest;
using StarryEyes.Breezy.Authorize;
using StarryEyes.Models;
using StarryEyes.Models.Backpanels.SystemEvents;
using StarryEyes.Models.Stores;
using StarryEyes.Models.Tab;

namespace StarryEyes.ViewModels.WindowParts.Flips.SearchFlip
{
    public class SearchCandidateViewModel : ViewModel
    {
        private readonly SearchFlipViewModel _parent;
        public SearchFlipViewModel Parent
        {
            get { return _parent; }
        }

        private bool _isSearchCandidateAvailable;
        public bool IsSearchCandidateAvailable
        {
            get { return _isSearchCandidateAvailable; }
            set
            {
                _isSearchCandidateAvailable = value;
                RaisePropertyChanged();
            }
        }

        public SearchCandidateViewModel(SearchFlipViewModel parent)
        {
            _parent = parent;
        }

        private long _currentId;

        private Uri _currentUserProfileImage;
        public Uri CurrentUserProfileImage
        {
            get { return _currentUserProfileImage; }
            set
            {
                _currentUserProfileImage = value;
                RaisePropertyChanged();
            }
        }

        private string _currentUserScreenName;
        public string CurrentUserScreenName
        {
            get { return _currentUserScreenName; }
            set
            {
                _currentUserScreenName = value;
                RaisePropertyChanged();
            }
        }

        private readonly ObservableCollection<SearchCandidateItemViewModel> _searchCandidates
            = new ObservableCollection<SearchCandidateItemViewModel>();
        public ObservableCollection<SearchCandidateItemViewModel> SearchCandidates
        {
            get { return _searchCandidates; }
        }

        public void UpdateInfo()
        {
            // update current binding accounts
            var ctab = MainAreaModel.CurrentFocusTab;
            long cid = 0;
            if (ctab != null && ctab.BindingAccountIds.Count == 1)
            {
                cid = ctab.BindingAccountIds.First();
            }
            if (_currentId != cid)
            {
                _currentId = cid;
                _searchCandidates.Clear();
                var aid = AccountsStore.GetAccountSetting(_currentId);
                if (aid == null)
                {
                    IsSearchCandidateAvailable = false;
                    return;
                }
                CurrentUserScreenName = aid.AuthenticateInfo.UnreliableScreenName;
                CurrentUserProfileImage = aid.AuthenticateInfo.UnreliableProfileImageUri;
                IsSearchCandidateAvailable = true;
                aid.AuthenticateInfo.GetSavedSearches()
                    .ObserveOnDispatcher()
                   .Subscribe(j => _searchCandidates.Add(new SearchCandidateItemViewModel(this, aid.AuthenticateInfo, j.id, j.query)), ex => BackpanelModel.RegisterEvent(new OperationFailedEvent(ex.Message)));
            }
        }
    }


    public class SearchCandidateItemViewModel : ViewModel
    {
        private readonly SearchCandidateViewModel _parent;
        private readonly AuthenticateInfo _authenticateInfo;
        private readonly long _id;
        private readonly string _query;

        public SearchCandidateItemViewModel(SearchCandidateViewModel parent,
            AuthenticateInfo authenticateInfo, long id, string query)
        {
            _parent = parent;
            _authenticateInfo = authenticateInfo;
            _id = id;
            _query = query;
        }

        public AuthenticateInfo AuthenticateInfo
        {
            get { return _authenticateInfo; }
        }

        public long Id
        {
            get { return _id; }
        }

        public string Query
        {
            get { return _query; }
        }

        public void SelectThis()
        {
            _parent.Parent.Text = Query;
        }

        #region RemoveCommand
        private Livet.Commands.ViewModelCommand _removeCommand;

        public Livet.Commands.ViewModelCommand RemoveCommand
        {
            get { return _removeCommand ?? (_removeCommand = new Livet.Commands.ViewModelCommand(Remove)); }
        }

        public void Remove()
        {
            _authenticateInfo.DestroySavedSearch(_id)
                             .Subscribe(_ => _parent.UpdateInfo(),
                                        ex => BackpanelModel.RegisterEvent(new OperationFailedEvent(ex.Message)));
        }
        #endregion
    }
}
