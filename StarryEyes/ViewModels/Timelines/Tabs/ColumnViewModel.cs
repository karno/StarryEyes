using System;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using Livet;
using StarryEyes.Albireo.Helpers;
using StarryEyes.Models;
using StarryEyes.Models.Timelines.Tabs;
using StarryEyes.ViewModels.WindowParts;

namespace StarryEyes.ViewModels.Timelines.Tabs
{
    public class ColumnViewModel : ViewModel
    {
        private readonly MainAreaViewModel _parent;
        private readonly ColumnModel _model;
        private readonly ReadOnlyDispatcherCollectionRx<TabViewModel> _tabs;
        public ReadOnlyDispatcherCollectionRx<TabViewModel> Tabs => _tabs;

        public MainAreaViewModel Parent => _parent;

        public ColumnModel Model => _model;

        public TabViewModel FocusedTab
        {
            get
            {
                if (_tabs == null || _tabs.Count == 0) return null;
                if (_tabs.Count <= _model.CurrentFocusTabIndex)
                {
                    _model.CurrentFocusTabIndex = 0;
                }
                return _tabs[_model.CurrentFocusTabIndex];
            }
            set
            {
                var index = _tabs.IndexOf(value);
                if (_model.CurrentFocusTabIndex == index) return;
                _model.CurrentFocusTabIndex = _tabs.IndexOf(value);
                _tabs.ForEach(item => item.UpdateFocus());
                RaisePropertyChanged();
            }
        }

        #region DragDrop Control

        private Livet.Commands.ViewModelCommand _dragDropStartCommand;

        public Livet.Commands.ViewModelCommand DragDropStartCommand =>
            _dragDropStartCommand ?? (_dragDropStartCommand = new Livet.Commands.ViewModelCommand(DragDropStart));

        public void DragDropStart()
        {
            Parent.StartDragDrop();
        }

        private Livet.Commands.ViewModelCommand _dragDropFinishCommand;

        public Livet.Commands.ViewModelCommand DragDropFinishCommand =>
            _dragDropFinishCommand ?? (_dragDropFinishCommand = new Livet.Commands.ViewModelCommand(DragDropFinish));

        public void DragDropFinish()
        {
            Parent.FinishDragDrop();
        }

        private bool _isDragDropping;

        public bool IsDragDropping
        {
            get => _isDragDropping;
            set
            {
                _isDragDropping = value;
                RaisePropertyChanged();
            }
        }

        private DropAcceptDescription _leftAccept;

        private DropAcceptDescription _rightAccept;

        public DropAcceptDescription LeftAcceptDescription
        {
            get
            {
                if (_leftAccept == null)
                {
                    _leftAccept = new DropAcceptDescription();
                    _leftAccept.DragOver += AcceptTabViewModel;
                    _leftAccept.DragDrop += e => DropCreateNewColumn(e, false);
                }
                return _leftAccept;
            }
        }

        public DropAcceptDescription RightAcceptDescription
        {
            get
            {
                if (_rightAccept == null)
                {
                    _rightAccept = new DropAcceptDescription();
                    _rightAccept.DragOver += AcceptTabViewModel;
                    _rightAccept.DragDrop += e => DropCreateNewColumn(e, true);
                }
                return _rightAccept;
            }
        }

        private void AcceptTabViewModel(DragEventArgs e)
        {
            var data = e.Data.GetData(typeof(TabViewModel)) as TabViewModel;
            e.Effects = data != null ? DragDropEffects.Copy : DragDropEffects.None;
        }

        private void DropCreateNewColumn(DragEventArgs e, bool createNext)
        {
            var data = e.Data.GetData(typeof(TabViewModel)) as TabViewModel;
            if (data == null) return;
            int fromColumnIndex, fromTabIndex;
            // get index
            var curindex = TabManager.FindColumnIndex(Model);
            if (curindex == -1) return;
            if (!TabManager.FindColumnTabIndex(data.Model, out fromColumnIndex, out fromTabIndex))
            {
                return;
            }
            var item = TabManager.Columns[fromColumnIndex].Tabs[fromTabIndex];
            TabManager.Columns[fromColumnIndex].RemoveTab(fromTabIndex);
            var index = createNext ? curindex + 1 : curindex;
            TabManager.CreateColumn(index, item);
            var focusTarget = TabManager.Columns[index];
            TabManager.CleanupColumn();
            var focusTargetIndex = TabManager.FindColumnIndex(focusTarget);
            if (focusTargetIndex >= 0 && focusTargetIndex < Parent.Columns.Count)
            {
                Parent.Columns[focusTargetIndex].Focus();
            }
        }

        #endregion DragDrop Control

        private DropAcceptDescription _description;

        public DropAcceptDescription DropAcceptDescription
        {
            get
            {
                if (_description == null)
                {
                    _description = new DropAcceptDescription();
                    _description.DragOver += e =>
                    {
                        var data = e.Data.GetData(typeof(TabViewModel)) as TabViewModel;
                        e.Effects = data != null ? DragDropEffects.Move : DragDropEffects.None;
                    };
                    _description.DragDrop += e =>
                    {
                        var data = e.Data.GetData(typeof(TabViewModel)) as TabViewModel;
                        if (data == null) return;
                        var dataPreviousParent = data.Parent;
                        var source = e.OriginalSource as FrameworkElement;
                        if (source == null) return;
                        int destColumnIndex, destTabIndex;
                        var tvm = source.DataContext as TabViewModel;
                        var cvm = source.DataContext as ColumnViewModel;
                        // find destination
                        if (tvm != null)
                        {
                            if (tvm == data) return;
                            destColumnIndex = TabManager.FindColumnIndex(Model);
                            destTabIndex = TabManager.FindTabIndex(tvm.Model, destColumnIndex);
                        }
                        else if (cvm != null)
                        {
                            destColumnIndex = TabManager.FindColumnIndex(Model);
                            destTabIndex = Model.Tabs.Count;
                        }
                        else
                        {
                            return;
                        }

                        int fromColumnIndex, fromTabIndex;
                        // get index
                        if (!TabManager.FindColumnTabIndex(data.Model, out fromColumnIndex, out fromTabIndex))
                        {
                            return;
                        }
                        // ensure move point
                        if (fromColumnIndex == destColumnIndex)
                        {
                            if (fromTabIndex < destTabIndex) destTabIndex--;
                            if (destTabIndex == -1) destTabIndex = 0;
                            if (destTabIndex == fromTabIndex) return;
                        }
                        // move tab
                        TabManager.MoveTo(fromColumnIndex, fromTabIndex, destColumnIndex, destTabIndex);
                        // update previous column's focus
                        if (Model != dataPreviousParent.Model && dataPreviousParent.Model.Tabs.Count > 0)
                        {
                            if (dataPreviousParent.Model.CurrentFocusTabIndex >= dataPreviousParent.Tabs.Count)
                            {
                                dataPreviousParent.Model.CurrentFocusTabIndex--;
                            }
                            else
                            {
                                // invoke update handler
                                dataPreviousParent.Model.CurrentFocusTabIndex =
                                    dataPreviousParent.Model.CurrentFocusTabIndex;
                            }
                        }
                        Model.CurrentFocusTabIndex = destTabIndex;
                        Focus();
                    };
                }
                return _description;
            }
        }

        private void UpdateFocusFromModel(int newFocus)
        {
            DispatcherHelper.UIDispatcher.InvokeAsync(() =>
            {
                _tabs.ForEach(item => item.UpdateFocus());
                RaisePropertyChanged(() => FocusedTab);
            });
        }

        public ColumnViewModel(MainAreaViewModel parent, ColumnModel model)
        {
            _parent = parent;
            _model = model;
            CompositeDisposable.Add(
                _tabs = ViewModelHelperRx.CreateReadOnlyDispatcherCollectionRx(
                    model.Tabs,
                    _ => new TabViewModel(this, _),
                    DispatcherHelper.UIDispatcher));
            CompositeDisposable.Add(
                Observable.FromEvent(
                              h => _model.CurrentFocusTabChanged += h,
                              h => _model.CurrentFocusTabChanged -= h)
                          .Select(_ => _model.CurrentFocusTabIndex)
                          .Subscribe(UpdateFocusFromModel));
            CompositeDisposable.Add(_tabs.ListenCollectionChanged(_ =>
                _tabs.ForEach(item => item.UpdateFocus())));
            if (_tabs.Count > 0)
            {
                FocusedTab = _tabs[0];
            }
        }

        public bool IsFocused => _parent.FocusedColumn == this;

        internal void UpdateFocus()
        {
            RaisePropertyChanged(() => IsFocused);
        }

        public void Focus()
        {
            _parent.FocusedColumn = this;
        }

        internal void CloseTab(TabViewModel tab)
        {
            _parent.CloseTab(this, tab);
            _parent.Columns.ForEach(cvm => cvm.RestoreLastClosedTabCommand.RaiseCanExecuteChanged());
        }

        #region CreateNewTabCommand

        private Livet.Commands.ViewModelCommand _createNewTabCommand;

        public Livet.Commands.ViewModelCommand CreateNewTabCommand =>
            _createNewTabCommand ?? (_createNewTabCommand = new Livet.Commands.ViewModelCommand(CreateNewTab));

        public void CreateNewTab()
        {
            var creating = TabModel.Create(String.Empty, null);
            MainWindowModel.ShowTabConfigure(creating)
                           .Subscribe(_ =>
                           {
                               if (String.IsNullOrEmpty(creating.Name) && creating.FilterQuery == null) return;
                               Model.CreateTab(creating);
                           });
        }

        #endregion CreateNewTabCommand

        #region RestoreLastClosedTabCommand

        private Livet.Commands.ViewModelCommand _restoreLastClosedTabCommand;

        public Livet.Commands.ViewModelCommand RestoreLastClosedTabCommand =>
            _restoreLastClosedTabCommand ?? (_restoreLastClosedTabCommand =
                new Livet.Commands.ViewModelCommand(RestoreLastClosedTab, CanRestoreClosedTab));

        public bool CanRestoreClosedTab()
        {
            return TabManager.CanReviveTab;
        }

        public void RestoreLastClosedTab()
        {
            Focus();
            if (TabManager.CanReviveTab)
            {
                TabManager.ReviveTab();
            }
            _parent.Columns.ForEach(cvm => cvm.RestoreLastClosedTabCommand.RaiseCanExecuteChanged());
        }

        #endregion RestoreLastClosedTabCommand
    }
}