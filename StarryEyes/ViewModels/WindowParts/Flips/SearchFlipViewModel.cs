using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Livet.EventListeners;
using Livet.Messaging;
using StarryEyes.Filters;
using StarryEyes.Filters.Parsing;
using StarryEyes.Globalization.WindowParts;
using StarryEyes.Models;
using StarryEyes.Models.Inputting;
using StarryEyes.Models.Timelines.SearchFlips;
using StarryEyes.Settings;
using StarryEyes.ViewModels.Timelines.SearchFlips;
using StarryEyes.ViewModels.WindowParts.Flips.SearchFlips;
using StarryEyes.Views.Utils;

namespace StarryEyes.ViewModels.WindowParts.Flips
{
    public class SearchFlipViewModel : PartialFlipViewModelBase
    {
        protected override bool IsWindowCommandsRelated
        {
            get { return false; }
        }

        private readonly SearchCandidateViewModel _candidateViewModel;
        private SearchResultViewModel _resultViewModel;
        private UserResultViewModel _userViewModel;
        private UserInfoViewModel _userInfoViewModel;

        public SearchFlipViewModel()
        {
            if (DesignTimeUtil.IsInDesignMode) return;
            _candidateViewModel = new SearchCandidateViewModel(this);
            CompositeDisposable.Add(
                Observable.FromEvent(
                    h => KeyAssignManager.KeyAssignChanged += h,
                    h => KeyAssignManager.KeyAssignChanged -= h)
                          .Subscribe(_ => RaisePropertyChanged(() => SearchHintLabel)));
            CompositeDisposable.Add(
                new EventListener<Action<string, SearchMode>>(
                    h => SearchFlipModel.SearchRequested += h,
                    h => SearchFlipModel.SearchRequested -= h,
                    (query, mode) =>
                    {
                        Open();
                        // to do nothing.
                        if (Text == query && SearchMode == mode) return;
                        Text = query;
                        if (SearchMode == mode)
                        {
                            CommitSearch();
                        }
                        else
                        {
                            SearchMode = mode;
                        }
                    }));
            CompositeDisposable.Add(
                new EventListener<Action>(
                    h => InputModel.FocusRequest += h,
                    h => InputModel.FocusRequest -= h,
                    CloseCore));
        }

        private bool _isSearchResultAvailable;
        public bool IsSearchResultAvailable
        {
            get { return _isSearchResultAvailable; }
            set
            {
                _isSearchResultAvailable = value;
                RaisePropertyChanged();
            }
        }

        public SearchCandidateViewModel SearchCandidate
        {
            get { return _candidateViewModel; }
        }
        public SearchResultViewModel SearchResult
        {
            get { return _resultViewModel; }
            private set
            {
                var previous = Interlocked.Exchange(ref _resultViewModel, value);
                RaisePropertyChanged();
                if (previous != null)
                {
                    previous.Dispose();
                }
            }
        }
        public UserResultViewModel UserResult
        {
            get { return _userViewModel; }
            private set
            {
                var previous = Interlocked.Exchange(ref _userViewModel, value);
                RaisePropertyChanged();
                if (previous != null)
                {
                    previous.Dispose();
                }
            }
        }
        public UserInfoViewModel UserInfo
        {
            get { return _userInfoViewModel; }
            private set
            {
                var previous = Interlocked.Exchange(ref _userInfoViewModel, value);
                RaisePropertyChanged();
                if (previous != null)
                {
                    previous.Dispose();
                }
            }
        }

        private bool _isQueryMode;
        public bool IsQueryMode
        {
            get { return _isQueryMode; }
            set
            {
                _isQueryMode = value;
                RaisePropertyChanged();
            }
        }

        private bool _canBeUserScreenName;
        public bool CanBeUserScreenName
        {
            get { return _canBeUserScreenName; }
            set
            {
                _canBeUserScreenName = value;
                RaisePropertyChanged();
            }
        }

        private bool _isSearchOptionAvailable;
        public bool IsSearchOptionAvailable
        {
            get { return _isSearchOptionAvailable; }
            set
            {
                _isSearchOptionAvailable = value;
                RaisePropertyChanged();
            }
        }

        private bool _displaySlimView;
        public bool DisplaySlimView
        {
            get { return _displaySlimView; }
            set
            {
                if (_displaySlimView == value) return;
                _displaySlimView = value;
                RaisePropertyChanged();
            }
        }

        public void NotifyResultWidthChanged(double width)
        {
            DisplaySlimView = width < 572;
        }

        #region Search options

        public void SetNextSearchOption()
        {
            if (SearchMode == SearchMode.UserScreenName)
                SearchMode = SearchMode.CurrentTab;
            else
                SearchMode = (SearchMode)(((int)SearchMode) + 1);
            if (SearchMode == SearchMode.UserScreenName && !CanBeUserScreenName)
                SearchMode = SearchMode.CurrentTab;
        }

        public void SetPreviousSearchOption()
        {
            if (SearchMode == SearchMode.CurrentTab)
                SearchMode = SearchMode.UserScreenName;
            else
                SearchMode = (SearchMode)(((int)SearchMode) - 1);
            if (SearchMode == SearchMode.UserScreenName && !CanBeUserScreenName)
                SearchMode = SearchMode.UserWeb;
        }

        public void SetLocalTabSearch()
        {
            SearchMode = SearchMode.CurrentTab;
        }

        public void SetLocalSearch()
        {
            SearchMode = SearchMode.Local;
        }

        public void SetWebSearch()
        {
            SearchMode = SearchMode.Web;
        }

        public void SetUserWebSearch()
        {
            SearchMode = SearchMode.UserWeb;
        }

        public void SetUserIdSearch()
        {
            SearchMode = SearchMode.UserScreenName;
        }

        #endregion Search options

        private string _text;
        public string Text
        {
            get { return _text; }
            set
            {
                if (_text != value)
                {
                    _text = value;
                    OnTextChanged(value);
                    RaisePropertyChanged();
                }
            }
        }

        private string _errorText;
        public string ErrorText
        {
            get { return _errorText; }
            set
            {
                _errorText = value;
                RaisePropertyChanged();
                RaisePropertyChanged(() => HasError);
            }
        }

        public bool HasError
        {
            get { return !String.IsNullOrEmpty(_errorText); }
        }

        private SearchMode _searchMode = SearchMode.CurrentTab;
        public SearchMode SearchMode
        {
            get { return _searchMode; }
            set
            {
                if (_searchMode == value) return;
                _searchMode = value;
                RaisePropertyChanged();
                CommitSearch();
            }
        }

        private string _searchHintKeyAssign;
        public string SearchHintLabel
        {
            get
            {
                if (_searchHintKeyAssign == null)
                {
                    var action = KeyAssignManager.CurrentProfile.FindAssignFromActionName("FocusToSearch")
                                                 .FirstOrDefault();
                    if (action != null)
                    {
                        _searchHintKeyAssign = " (" + action.GetKeyDescribeString() + ")";
                    }
                    else
                    {
                        _searchHintKeyAssign = String.Empty;
                    }
                }
                return "search" + _searchHintKeyAssign;
            }
        }

        private readonly Regex _userScreenNameRegex = new Regex("^@?[A-Za-z0-9_]+$", RegexOptions.Compiled);
        private async void OnTextChanged(string value)
        {
            if (value != null && value.StartsWith("?"))
            {
                IsQueryMode = true;
                IsSearchOptionAvailable = false;
                IsSearchResultAvailable = false;
                ErrorText = SearchFlipResources.SearchFlipQueryCompiling;
                try
                {
                    if (value == "?")
                    {
                        ErrorText = SearchFlipResources.SearchFlipQueryEmpty;
                        IsSearchResultAvailable = false;
                        return;
                    }
                    await Task.Run(() =>
                    {
                        var result = QueryCompiler.Compile(value.Substring(1));
                        result.GetEvaluator(); // check evaluator
                    });
                    ErrorText = null;
                }
                catch (FilterQueryException fex)
                {
                    ErrorText = fex.Message;
                }
            }
            else
            {
                IsQueryMode = false;
                IsSearchOptionAvailable = true;
                ErrorText = null;
                if (String.IsNullOrEmpty(value))
                {
                    IsSearchResultAvailable = false;
                    IsSearchOptionAvailable = false;
                }
                else
                {
                    IsSearchResultAvailable = SearchMode == SearchMode.CurrentTab;
                    if (IsSearchResultAvailable)
                    {
                        CommitSearch();
                    }
                    CanBeUserScreenName = _userScreenNameRegex.IsMatch(value);
                }
            }
        }

        public override void Open()
        {
            if (IsVisible) return;
            base.Open();
            SearchMode = SearchMode.CurrentTab;
            SearchCandidate.UpdateInfo();
        }

        public override void Close()
        {
            if (!IsVisible) return;
            CloseCore();
            MainWindowModel.SetFocusTo(FocusRequest.Timeline);
        }

        private void CloseCore()
        {
            Text = String.Empty;
            IsSearchResultAvailable = false;
            SearchResult = null;
            UserResult = null;
            UserInfo = null;
            base.Close();
            _backStack.Clear();
        }

        #region Text box control

        public void FocusToSearchBox()
        {
            Messenger.RaiseSafe(() => new InteractionMessage("FocusToTextBox"));
        }

        public void GotFocusToSearchBox()
        {
            Open();
        }

        public void OnEnterKeyDown()
        {
            // check difference
            if ((_previousCommit != Text || !IsSearchResultAvailable) &&
                (!IsQueryMode || ErrorText == null))
            {
                // commit search query
                CommitSearch();
            }
            if (SearchResult != null)
            {
                SearchResult.SetFocus();
            }
        }

        #endregion Text box control

        private readonly Stack<Tuple<string, SearchMode>> _backStack = new Stack<Tuple<string, SearchMode>>();
        private string _previousCommit;
        private void CommitSearch()
        {
            _previousCommit = Text;
            if (_backStack.Count > 0 && (_backStack.Peek().Item1 == Text || _backStack.Peek().Item2 == SearchMode.CurrentTab))
            {
                _backStack.Pop();
            }
            _backStack.Push(Tuple.Create(Text, SearchMode));
            if (String.IsNullOrWhiteSpace(Text))
            {
                IsSearchResultAvailable = false;
                return;
            }
            SearchResult = null;
            UserResult = null;
            UserInfo = null;
            if (IsQueryMode)
            {
                try
                {
                    ShowSearchResult(new SearchResultModel(Text.Substring(1), SearchOption.Query));
                }
                catch (FilterQueryException)
                {
                    // query contains errors
                    IsSearchResultAvailable = false;
                    return;
                }
            }
            else
            {
                switch (SearchMode)
                {
                    case SearchMode.CurrentTab:
                        ShowSearchResult(new SearchResultModel(Text, SearchOption.CurrentTab));
                        break;
                    case SearchMode.Local:
                        ShowSearchResult(new SearchResultModel(Text, SearchOption.Local));
                        break;
                    case SearchMode.Web:
                        ShowSearchResult(new SearchResultModel(Text, SearchOption.Web));
                        break;
                    case SearchMode.UserWeb:
                        UserResult = new UserResultViewModel(this, Text);
                        break;
                    case SearchMode.UserScreenName:
                        UserInfo = new UserInfoViewModel(this, Text);
                        break;
                    default:
                        IsSearchResultAvailable = false;
                        throw new ArgumentOutOfRangeException();
                }
            }
            IsSearchResultAvailable = true;
        }

        private void ShowSearchResult(SearchResultModel model)
        {
            SearchResult = new SearchResultViewModel(this, model);
        }

        public void CloseResults()
        {
            IsSearchResultAvailable = false;
            SearchResult = null;
            UserResult = null;
            UserInfo = null;
            FocusToSearchBox();
        }

        public void RewindStack()
        {
            _backStack.Pop();
            if (_backStack.Count <= 0)
            {
                _backStack.Clear();
                Close();
                return;
            }
            var item = _backStack.Peek();
            Text = item.Item1;
            if (SearchMode == item.Item2)
            {
                CommitSearch();
            }
            else
            {
                SearchMode = item.Item2;
            }
        }
    }
}