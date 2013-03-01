using System;
using System.Reactive.Linq;
using System.Threading;
using StarryEyes.ViewModels.WindowParts.Flips.SearchFlip;

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
            _candidateViewModel = new SearchCandidateViewModel();
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

        private bool _isQueryEnabled;
        public bool IsQueryEnabled
        {
            get { return _isQueryEnabled; }
            set
            {
                _isQueryEnabled = value;
                RaisePropertyChanged();
            }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get
            {
                return _isLoading;
            }
            set
            {
                _isLoading = value;
                RaisePropertyChanged();
            }
        }

        private string _query;
        public string Query
        {
            get { return _query; }
            set
            {
                if (_query != value)
                {
                    _query = value;
                    OnUpdateQuery(value);
                    RaisePropertyChanged();
                }
            }
        }

        private volatile uint _cqt;
        private void OnUpdateQuery(string query)
        {
            var queryTicket = ++_cqt;
            if (String.IsNullOrEmpty(query))
            {
                IsQueryEnabled = false;
                IsLoading = false;
                SearchResult = null;
                return;
            }
            IsLoading = true;
            Observable.Timer(TimeSpan.FromMilliseconds(250))
                      .Subscribe(_ =>
                      {
                          if (_cqt == queryTicket)
                          {
                              // commit query
                              IsQueryEnabled = true;
                              IsLoading = false;
                              SearchResult = new SearchResultViewModel(query);
                          }
                      });
        }
    }
}
