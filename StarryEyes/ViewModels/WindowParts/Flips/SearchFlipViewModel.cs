using System;
using System.Reactive.Linq;
using System.Threading;
using StarryEyes.Filters;
using StarryEyes.Filters.Parsing;
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
    }
}
