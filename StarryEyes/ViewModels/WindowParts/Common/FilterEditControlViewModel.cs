using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Livet;
using StarryEyes.Filters;

namespace StarryEyes.ViewModels.WindowParts.Common
{
    public class FilterEditControlViewModel : ViewModel
    {
        private FilterQuery _filter;
        public FilterQuery Filter
        {
            get { return _filter; }
            set
            {
                _filter = value;
                RaisePropertyChanged();
            }
        }

        private readonly DispatcherCollection<FilterSourceViewModel> _sources
            = new DispatcherCollection<FilterSourceViewModel>(DispatcherHelper.UIDispatcher);

        public DispatcherCollection<FilterSourceViewModel> Sources
        {
            get { return _sources; }
        }

        public FilterEditControlViewModel()
        {
        }

    }
}
