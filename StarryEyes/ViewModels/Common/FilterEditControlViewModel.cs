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
            get { return this._filter; }
            set
            {
                this._filter = value;
                this.RaisePropertyChanged();
            }
        }

        private readonly DispatcherCollection<FilterSourceViewModel> _sources
            = new DispatcherCollection<FilterSourceViewModel>(DispatcherHelper.UIDispatcher);

        public DispatcherCollection<FilterSourceViewModel> Sources
        {
            get { return this._sources; }
        }

        public FilterEditControlViewModel()
        {
        }

    }
}
