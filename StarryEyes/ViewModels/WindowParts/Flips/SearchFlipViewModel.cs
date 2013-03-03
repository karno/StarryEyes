using System;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Livet.Messaging;
using StarryEyes.Filters;
using StarryEyes.Filters.Parsing;
using StarryEyes.Models;
using StarryEyes.ViewModels.WindowParts.Flips.SearchFlip;
using StarryEyes.Views.Messaging;
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

        private async void OnTextChanged(string value)
        {
            if (value != null && value.StartsWith("?"))
            {
                IsQueryMode = true;
                try
                {
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
                }
            }
        }

        public void Open()
        {
            this.Messenger.Raise(new GoToStateMessage("Open"));
        }

        public void Close()
        {
            Text = String.Empty;
            this.Messenger.Raise(new GoToStateMessage("Close"));
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

        #endregion
    }
}
