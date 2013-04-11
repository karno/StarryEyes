using System;
using System.Linq;
using System.Reactive.Linq;
using Livet;
using StarryEyes.Models;
using StarryEyes.Models.Tab;

namespace StarryEyes.ViewModels.WindowParts.Timelines
{
    public class ColumnViewModel : ViewModel
    {
        private readonly MainAreaViewModel _parent;
        private readonly ColumnModel _model;
        private readonly ReadOnlyDispatcherCollectionRx<TabViewModel> _tabs;
        public ReadOnlyDispatcherCollectionRx<TabViewModel> Tabs
        {
            get { return _tabs; }
        }

        public ColumnModel Model
        {
            get { return _model; }
        }

        private int _currentFocus;
        public TabViewModel FocusedTab
        {
            get { return _tabs != null && _tabs.Count > 0 ? _tabs[_model.CurrentFocusTabIndex] : null; }
            set
            {
                _model.CurrentFocusTabIndex = _currentFocus = _tabs.IndexOf(value);
                _tabs.ForEach(item => item.UpdateFocus());
                RaisePropertyChanged();
            }
        }

        private void UpdateFocusFromModel(int newFocus)
        {
            DispatcherHolder.Enqueue(() =>
            {
                _currentFocus = newFocus;
                _tabs.ForEach(item => item.UpdateFocus());
                RaisePropertyChanged(() => FocusedTab);
            });
        }

        public ColumnViewModel(MainAreaViewModel parent, ColumnModel model)
        {
            _parent = parent;
            _model = model;
            this.CompositeDisposable.Add(
                _tabs = ViewModelHelperRx.CreateReadOnlyDispatcherCollectionRx(
                    model.Tabs,
                    _ => new TabViewModel(this, _),
                    DispatcherHelper.UIDispatcher));
            this.CompositeDisposable.Add(
                Observable.FromEvent(
                h => _model.OnCurrentFocusTabChanged += h,
                h => _model.OnCurrentFocusTabChanged -= h)
                .Select(_ => _model.CurrentFocusTabIndex)
                .Subscribe(UpdateFocusFromModel));
            if (_tabs.Count > 0)
            {
                FocusedTab = _tabs[0];
            }
        }

        public bool IsFocused
        {
            get { return _parent.FocusedColumn == this; }
        }

        internal void UpdateFocus()
        {
            this.RaisePropertyChanged(() => IsFocused);
        }

        public void Focus()
        {
            _parent.FocusedColumn = this;
        }

        internal void CloseTab(TabViewModel tab)
        {
            _parent.CloseTab(this, tab);
        }

        #region CreateNewTabCommand
        private Livet.Commands.ViewModelCommand _createNewTabCommand;

        public Livet.Commands.ViewModelCommand CreateNewTabCommand
        {
            get
            {
                return _createNewTabCommand ??
                       (_createNewTabCommand = new Livet.Commands.ViewModelCommand(CreateNewTab));
            }
        }

        const string DefaultQueryString = "from local where ()";
        public void CreateNewTab()
        {
            var creating = new TabModel(string.Empty, DefaultQueryString);
            IDisposable subscribe = null;
            subscribe = Observable.FromEvent<bool>(
                h => creating.OnConfigurationUpdated += h,
                h => creating.OnConfigurationUpdated -= h)
                                  .Subscribe(_ =>
                                  {
                                      if (subscribe != null) subscribe.Dispose();
                                      // configuration completed.
                                      if (String.IsNullOrEmpty(creating.Name) &&
                                          creating.FilterQueryString == DefaultQueryString) return;
                                      this.Model.CreateTab(creating);
                                  });
            MainWindowModel.ShowTabConfigure(creating);
        }
        #endregion

        #region RestoreLastClosedTabCommand
        private Livet.Commands.ViewModelCommand _restoreLastClosedTabCommand;

        public Livet.Commands.ViewModelCommand RestoreLastClosedTabCommand
        {
            get
            {
                return _restoreLastClosedTabCommand ??
                       (_restoreLastClosedTabCommand = new Livet.Commands.ViewModelCommand(RestoreLastClosedTab));
            }
        }

        public void RestoreLastClosedTab()
        {
            Focus();
            TabManager.ReviveTab();
        }
        #endregion
    }
}
