using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using Cadena.Api.Rest;
using Livet;
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
        public SearchFlipViewModel Parent { get; }

        private bool _isSearchCandidateAvailable;

        public bool IsSearchCandidateAvailable
        {
            get => _isSearchCandidateAvailable;
            set
            {
                _isSearchCandidateAvailable = value;
                RaisePropertyChanged();
            }
        }

        public SearchCandidateViewModel(SearchFlipViewModel parent)
        {
            Parent = parent;
        }

        private long _currentId;

        private Uri _currentUserProfileImage;

        public Uri CurrentUserProfileImage
        {
            get => _currentUserProfileImage;
            set
            {
                _currentUserProfileImage = value;
                RaisePropertyChanged();
            }
        }

        private string _currentUserScreenName;

        public string CurrentUserScreenName
        {
            get => _currentUserScreenName;
            set
            {
                _currentUserScreenName = value;
                RaisePropertyChanged();
            }
        }

        public ObservableCollection<SearchCandidateItemViewModel> SearchCandidates { get; } =
            new ObservableCollection<SearchCandidateItemViewModel>();

        public async void UpdateInfo()
        {
            // update current binding accounts
            var ctab = TabManager.CurrentFocusTab;
            long cid = 0;
            if (ctab != null && ctab.BindingAccounts.Count == 1)
            {
                cid = ctab.BindingAccounts.First();
            }
            if (_currentId == cid) return;
            _currentId = cid;
            SearchCandidates.Clear();
            var aid = Setting.Accounts.Get(_currentId);
            if (aid == null)
            {
                IsSearchCandidateAvailable = false;
                return;
            }
            CurrentUserScreenName = aid.UnreliableScreenName;
            CurrentUserProfileImage = aid.UnreliableProfileImage;
            IsSearchCandidateAvailable = true;
            try
            {
                var searches = await aid.CreateAccessor().GetSavedSearchesAsync(CancellationToken.None);
                searches.Result.ForEach(s => SearchCandidates.Add(
                    new SearchCandidateItemViewModel(this, aid, s.Id, s.Query)));
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

        public SearchCandidateItemViewModel(SearchCandidateViewModel parent,
            TwitterAccount account, long id, string query)
        {
            _parent = parent;
            TwitterAccount = account;
            Id = id;
            Query = query;
        }

        public TwitterAccount TwitterAccount { get; }

        public long Id { get; }

        public string Query { get; }

        public void SelectThis()
        {
            _parent.Parent.Text = Query;
            _parent.Parent.SearchMode = SearchMode.Web;
            _parent.Parent.OnEnterKeyDown();
        }

        #region RemoveCommand

        private Livet.Commands.ViewModelCommand _removeCommand;

        public Livet.Commands.ViewModelCommand RemoveCommand => _removeCommand ??
                                                                (_removeCommand =
                                                                    new Livet.Commands.ViewModelCommand(Remove));

        public async void Remove()
        {
            try
            {
                await TwitterAccount.CreateAccessor().DestroySavedSearchAsync(Id, CancellationToken.None);
            }
            catch (Exception ex)
            {
                BackstageModel.RegisterEvent(new OperationFailedEvent(SearchFlipResources.InfoDeleteQueryFailed, ex));
            }
        }

        #endregion RemoveCommand
    }
}