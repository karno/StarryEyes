using System;
using System.Reactive.Linq;
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
            CompositeDisposable.Add(
                _columns =
                ViewModelHelper.CreateReadOnlyDispatcherCollection(
                    MainAreaModel.Columns,
                    cm => new ColumnViewModel(this, cm),
                    DispatcherHelper.UIDispatcher));
            CompositeDisposable.Add(
                Observable.FromEvent(
                    h => MainAreaModel.OnCurrentFocusColumnChanged += h,
                    h => MainAreaModel.OnCurrentFocusColumnChanged -= h)
                          .Select(_ => MainAreaModel.CurrentFocusColumn)
                          .Subscribe(UpdateFocusFromModel));
        }

        public ReadOnlyDispatcherCollection<ColumnViewModel> Columns
        {
            get { return _columns; }
        }

        private int _oldFocus;
        public ColumnViewModel Focused
        {
            get { return _columns[MainAreaModel.CurrentFocusColumn]; }
            set
            {
                var previous = Focused;
                MainAreaModel.CurrentFocusColumn = _oldFocus = _columns.IndexOf(value);
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