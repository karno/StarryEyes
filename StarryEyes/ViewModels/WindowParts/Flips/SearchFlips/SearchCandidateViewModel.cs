using System;
using System.Collections.ObjectModel;
using System.Linq;
using Livet;
using StarryEyes.Anomaly.TwitterApi.Rest;
using StarryEyes.Globalization;
using StarryEyes.Globalization.WindowParts;
using StarryEyes.Models;
using StarryEyes.Models.Accounting;
using StarryEyes.Models.Backstages.NotificationEvents;
using StarryEyes.Models.Timelines.Tabs;
using StarryEyes.Settings;

namespace StarryEyes.ViewModels.WindowParts.Flips.SearchFlips
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

        public async void UpdateInfo()
        {
            // update current binding accounts
            var ctab = TabManager.CurrentFocusTab;
            long cid = 0;
            if (ctab != null && ctab.BindingAccounts.Count == 1)
            {
                cid = ctab.BindingAccounts.First();
            }
            if (this._currentId == cid) return;
            this._currentId = cid;
            this._searchCandidates.Clear();
            var aid = Setting.Accounts.Get(this._currentId);
            if (aid == null)
            {
                this.IsSearchCandidateAvailable = false;
                return;
            }
            this.CurrentUserScreenName = aid.UnreliableScreenName;
            this.CurrentUserProfileImage = aid.UnreliableProfileImage;
            this.IsSearchCandidateAvailable = true;
            try
            {
                var searches = await aid.GetSavedSearchesAsync();
                searches.ForEach(s => this._searchCandidates.Add(
                    new SearchCandidateItemViewModel(this, aid, s.Item1, s.Item2)));
            }
            catch (Exception ex)
            {
                BackstageModel.RegisterEvent(new OperationFailedEvent(
                    SearchFlipResources.InfoSavedQueriesReceiveFailedFormat.SafeFormat(aid.UnreliableScreenName),
                    ex));
            }
        }
    }


    public class SearchCandidateItemViewModel : ViewModel
    {
        private readonly SearchCandidateViewModel _parent;
        private readonly TwitterAccount _account;
        private readonly long _id;
        private readonly string _query;

        public SearchCandidateItemViewModel(SearchCandidateViewModel parent,
            TwitterAccount account, long id, string query)
        {
            _parent = parent;
            this._account = account;
            _id = id;
            _query = query;
        }

        public TwitterAccount TwitterAccount
        {
            get { return this._account; }
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
            _parent.Parent.SearchMode = SearchMode.Web;
            _parent.Parent.OnEnterKeyDown();
        }

        #region RemoveCommand
        private Livet.Commands.ViewModelCommand _removeCommand;

        public Livet.Commands.ViewModelCommand RemoveCommand
        {
            get { return _removeCommand ?? (_removeCommand = new Livet.Commands.ViewModelCommand(Remove)); }
        }

        public async void Remove()
        {
            try
            {
                await this._account.DestroySavedSearchAsync(_id);
            }
            catch (Exception ex)
            {
                BackstageModel.RegisterEvent(new OperationFailedEvent(SearchFlipResources.InfoDeleteQueryFailed, ex));
            }
        }
        #endregion
    }
}
