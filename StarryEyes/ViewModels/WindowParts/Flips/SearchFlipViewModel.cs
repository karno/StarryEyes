using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Livet.Messaging;
using StarryEyes.Filters;
using StarryEyes.Filters.Parsing;
using StarryEyes.Models;
using StarryEyes.ViewModels.WindowParts.Flips.SearchFlip;
using StarryEyes.Views.Utils;

namespace StarryEyes.ViewModels.WindowParts.Flips
{
    public class SearchFlipViewModel : PartialFlipViewModelBase
    {
        protected override bool IsWindowCommandsRelated
        {
            get { return false; }
        }

        private SearchResultViewModel _resultViewModel;

        private readonly SearchCandidateViewModel _candidateViewModel;

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

        private bool _isUserSearchAvailable;
        public bool IsUserSearchAvailable
        {
            get { return _isUserSearchAvailable; }
            set
            {
                _isUserSearchAvailable = value;
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
                _searchMode = value;
                RaisePropertyChanged();
            }
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
                    IsSearchOptionAvailable = true;
                    IsUserSearchAvailable = _userScreenNameRegex.IsMatch(value);
                }
            }
        }

        public override void Open()
        {
            base.Open();
            SearchCandidate.UpdateInfo();
        }

        public override void Close()
        {
            MainWindowModel.SetFocusTo(FocusRequest.Timeline);
            base.Close();
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
            if (!IsQueryMode || ErrorText == null)
            {
                // commit search query
                IsSearchResultAvailable = true;
                CommitSearch();
            }
        }

        private void CommitSearch()
        {

        }

        #endregion
    }

    public enum SearchMode
    {
        Quick,
        Local,
        Web,
        UserWeb,
        UserId,
    }
}
