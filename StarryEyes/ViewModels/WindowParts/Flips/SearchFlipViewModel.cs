using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Livet.Messaging;
using StarryEyes.Filters;
using StarryEyes.Filters.Parsing;
using StarryEyes.Models;
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
        private UserCandidateViewModel _userViewModel;
        private UserInfoViewModel _userInfoViewModel;

        public SearchFlipViewModel()
        {
            if (DesignTimeUtil.IsInDesignMode) return;
            _candidateViewModel = new SearchCandidateViewModel(this);
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
        public UserCandidateViewModel UserCandidate
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
                SearchMode = SearchMode.Quick;
            else
                SearchMode = (SearchMode)(((int)SearchMode) + 1);
            if (SearchMode == SearchMode.UserScreenName && !CanBeUserScreenName)
                SearchMode = SearchMode.Quick;
        }

        public void SetPreviousSearchOption()
        {
            if (SearchMode == SearchMode.Quick)
                SearchMode = SearchMode.UserScreenName;
            else
                SearchMode = (SearchMode)(((int)SearchMode) - 1);
            if (SearchMode == SearchMode.UserScreenName && !CanBeUserScreenName)
                SearchMode = SearchMode.UserWeb;
        }

        public void SetQuickSearch()
        {
            SearchMode = SearchMode.Quick;
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

        private SearchMode _searchMode = SearchMode.Quick;
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

        public string SearchHintLabel
        {
            get { return "search (Ctrl+Q)"; }
        }

        private readonly Regex _userScreenNameRegex = new Regex("^[A-Za-z0-9_]+$", RegexOptions.Compiled);
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
                    IsSearchResultAvailable = SearchMode == SearchMode.Quick;
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
            SearchMode = SearchMode.Quick;
            SearchCandidate.UpdateInfo();
        }

        public override void Close()
        {
            if (!this.IsVisible) return;
            Text = String.Empty;
            base.Close();
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
                SearchResult.SetPhysicalFocus();
            }
        }

        #endregion

        private string _previousCommit;
        private void CommitSearch()
        {
            _previousCommit = Text;
            if (String.IsNullOrWhiteSpace(Text))
            {
                IsSearchResultAvailable = false;
                return;
            }
            SearchResult = null;
            UserCandidate = null;
            UserInfo = null;
            if (IsQueryMode)
            {
                SearchResult = new SearchResultViewModel(this, Text.Substring(1), SearchOption.Query);
            }
            else
            {
                switch (SearchMode)
                {
                    case SearchMode.Quick:
                        SearchResult = new SearchResultViewModel(this, Text, SearchOption.Quick);
                        break;
                    case SearchMode.Local:
                        SearchResult = new SearchResultViewModel(this, Text, SearchOption.None);
                        break;
                    case SearchMode.Web:
                        SearchResult = new SearchResultViewModel(this, Text, SearchOption.Web);
                        break;
                    case SearchMode.UserWeb:
                        UserCandidate = new UserCandidateViewModel(Text);
                        break;
                    case SearchMode.UserScreenName:
                        break;
                    default:
                        IsSearchResultAvailable = false;
                        throw new ArgumentOutOfRangeException();
                }
            }
            IsSearchResultAvailable = true;
        }
    }

    public enum SearchMode
    {
        Quick,
        Local,
        Web,
        UserWeb,
        UserScreenName,
    }
}
