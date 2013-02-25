using System;
using System.Reactive.Linq;
using Livet;
using StarryEyes.Models.Tab;
using StarryEyes.ViewModels.WindowParts.Timelines;

namespace StarryEyes.ViewModels.WindowParts
{
    public class MainAreaViewModel : ViewModel
    {
        private readonly ReadOnlyDispatcherCollectionRx<ColumnViewModel> _columns;

        public MainAreaViewModel()
        {
            CompositeDisposable.Add(
                _columns =
                ViewModelHelperRx.CreateReadOnlyDispatcherCollectionRx(
                    MainAreaModel.Columns,
                    cm => new ColumnViewModel(this, cm),
                    DispatcherHelper.UIDispatcher));
            CompositeDisposable.Add(
                Observable.FromEvent(
                    h => MainAreaModel.OnCurrentFocusColumnChanged += h,
                    h => MainAreaModel.OnCurrentFocusColumnChanged -= h)
                          .Select(_ => MainAreaModel.CurrentFocusColumnIndex)
                          .Subscribe(UpdateFocusFromModel));
        }

        public ReadOnlyDispatcherCollectionRx<ColumnViewModel> Columns
        {
            get { return _columns; }
        }

        private int _oldFocus;
        public ColumnViewModel Focused
        {
            get { return _columns[MainAreaModel.CurrentFocusColumnIndex]; }
            set
            {
                var previous = Focused;
                MainAreaModel.CurrentFocusColumnIndex = _oldFocus = _columns.IndexOf(value);
                previous.UpdateFocus();
                value.UpdateFocus();
                RaisePropertyChanged();
            }
        }

        private void UpdateFocusFromModel(int newFocus)
        {
            DispatcherHolder.Enqueue(() =>
            {
                if (newFocus == _oldFocus) return;
                _columns[_oldFocus].UpdateFocus();
                _columns[newFocus].UpdateFocus();
                _oldFocus = newFocus;
                RaisePropertyChanged(() => Focused);
            });
        }
    }
}
