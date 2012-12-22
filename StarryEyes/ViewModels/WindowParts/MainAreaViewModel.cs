using Livet;
using StarryEyes.Models.Tab;
using StarryEyes.ViewModels.WindowParts.Timelines;

namespace StarryEyes.ViewModels.WindowParts
{
    public class MainAreaViewModel : ViewModel
    {
        private readonly ReadOnlyDispatcherCollection<ColumnViewModel> _columns;

        public MainAreaViewModel()
        {
            CompositeDisposable.Add(_columns =
                                    ViewModelHelper.CreateReadOnlyDispatcherCollection(
                                        TabManager.Tabs,
                                        _ =>
                                        new ColumnViewModel(_),
                                        DispatcherHelper
                                            .UIDispatcher));
        }

        public ReadOnlyDispatcherCollection<ColumnViewModel> Columns
        {
            get { return _columns; }
        }
    }
}