using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Livet.Messaging;
using StarryEyes.Filters;
using StarryEyes.Filters.Parsing;
using StarryEyes.Models;
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
            this.CompositeDisposable.Add(
                Observable.FromEvent(
                    h => KeyAssignManager.KeyAssignChanged += h,
                    h => KeyAssignManager.KeyAssignChanged -= h)
                          .Subscribe(_ => RaisePropertyChanged(() => SearchHintLabel)));
            this.CompositeDisposable.Add(
                new Livet.EventListeners.EventListener<Action<string, SearchMode>>(
                    h => SearchFlipModel.SearchRequested += h,
                    h => SearchFlipModel.SearchRequested -= h,
                    (query, mode) =>
                    {
                        this.Open();
                        // to do nothing.
                        if (Text == query && SearchMode == mode) return;
                        Text = query;
                        if (SearchMode == mode)
                        {
                            this.CommitSearch();
                        }
                        else
                        {
                            SearchMode = mode;
                        }
                    }));
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

        #endregion

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
                try
                {
                    if (value == "?")
                    {
                        ErrorText = "クエリの本文がありません。";
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
            if (this.IsVisible) return;
            base.Open();
            SearchMode = SearchMode.CurrentTab;
            SearchCandidate.UpdateInfo();
        }

        public override void Close()
        {
            if (!this.IsVisible) return;
            Text = String.Empty;
            this.CloseResults();
            base.Close();
            _backStack.Clear();
            MainWindowModel.SetFocusTo(FocusRequest.Timeline);
        }

        #region Text box control

        public void FocusToSearchBox()
        {
            this.Messenger.Raise(new InteractionMessage("FocusToTextBox"));
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

        #endregion

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
            this.SearchResult = null;
            this.UserResult = null;
            this.UserInfo = null;
            if (IsQueryMode)
            {
                this.ShowSearchResult(new SearchResultModel(Text.Substring(1), SearchOption.Query));
            }
            else
            {
                switch (SearchMode)
                {
                    case SearchMode.CurrentTab:
                        this.ShowSearchResult(new SearchResultModel(Text, SearchOption.CurrentTab));
                        break;
                    case SearchMode.Local:
                        this.ShowSearchResult(new SearchResultModel(Text, SearchOption.Local));
                        break;
                    case SearchMode.Web:
                        this.ShowSearchResult(new SearchResultModel(Text, SearchOption.Web));
                        break;
                    case SearchMode.UserWeb:
                        this.UserResult = new UserResultViewModel(this, Text);
                        break;
                    case SearchMode.UserScreenName:
                        this.UserInfo = new UserInfoViewModel(this, Text);
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
            this.SearchResult = null;
            this.UserResult = null;
            this.UserInfo = null;
            this.FocusToSearchBox();
        }

        public void RewindStack()
        {
            if (_backStack.Count <= 1)
            {
                _backStack.Clear();
                this.Close();
                return;
            }
            _backStack.Pop();
            var item = _backStack.Peek();
            this.Text = item.Item1;
            if (this.SearchMode == item.Item2)
            {
                this.CommitSearch();
            }
            else
            {
                this.SearchMode = item.Item2;
            }
        }
    }
}
