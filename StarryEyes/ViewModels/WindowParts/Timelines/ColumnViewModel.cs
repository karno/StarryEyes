using System;
using System.Reactive.Linq;
using Livet;
using StarryEyes.Models.Tab;

namespace StarryEyes.ViewModels.WindowParts.Timelines
{
    public class ColumnViewModel : ViewModel
    {
        private readonly MainAreaViewModel _parent;
        private readonly ColumnModel _model;
        private readonly ReadOnlyDispatcherCollection<TabViewModel> _tabs;
        public ReadOnlyDispatcherCollection<TabViewModel> Tabs
        {
            get { return _tabs; }
        }

        private int _oldFocus;
        public TabViewModel Focused
        {
            get { return _tabs[_model.CurrentFocusTabIndex]; }
            set
            {
                var previous = Focused;
                _model.CurrentFocusTabIndex = _oldFocus = _tabs.IndexOf(value);
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
                _tabs[_oldFocus].UpdateFocus();
                _tabs[newFocus].UpdateFocus();
                _oldFocus = newFocus;
                RaisePropertyChanged(() => Focused);
            });
        }

        public ColumnViewModel(MainAreaViewModel parent, ColumnModel model)
        {
            _parent = parent;
            _model = model;
            this.CompositeDisposable.Add(
                _tabs = ViewModelHelper.CreateReadOnlyDispatcherCollection(
                    model.Tabs,
                    _ => new TabViewModel(this, _),
                    DispatcherHelper.UIDispatcher));
            this.CompositeDisposable.Add(
                Observable.FromEvent(
                h => _model.OnCurrentFocusColumnChanged += h,
                h => _model.OnCurrentFocusColumnChanged -= h)
                .Select(_ => _model.CurrentFocusTabIndex)
                .Subscribe(UpdateFocusFromModel));
            if (_tabs.Count > 0)
            {
                Focused = _tabs[0];
            }
        }

        public void AddTab(TabViewModel tab)
        {
            this._model.Tabs.Add(tab.Model);
            this.Focused = tab;
        }

        public bool IsFocused
        {
            get { return _parent.Focused == this; }
        }

        internal void UpdateFocus()
        {
            this.RaisePropertyChanged(() => IsFocused);
        }

        public void Focus()
        {
            _parent.Focused = this;
        }
    }
}
