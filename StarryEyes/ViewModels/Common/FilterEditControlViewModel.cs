using Livet;
using StarryEyes.Filters;
using StarryEyes.ViewModels.Common.FilterEditor;

namespace StarryEyes.ViewModels.Common
{
    public class FilterEditControlViewModel : ViewModel
    {
        private FilterQuery _filter;

        public FilterQuery Filter
        {
            get => _filter;
            set
            {
                _filter = value;
                RaisePropertyChanged();
            }
        }

        public DispatcherCollection<FilterSourceViewModel> Sources { get; } =
            new DispatcherCollection<FilterSourceViewModel>(DispatcherHelper.UIDispatcher);

        public FilterEditControlViewModel()
        {
        }
    }
}