using System;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using Livet;
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
        public ReadOnlyDispatcherCollectionRx<TabViewModel> Tabs
        {
            get { return this._tabs; }
        }

        public MainAreaViewModel Parent
        {
            get { return this._parent; }
        }

        public ColumnModel Model
        {
            get { return this._model; }
        }

        public TabViewModel FocusedTab
        {
            get
            {
                if (this._tabs == null || this._tabs.Count == 0) return null;
                if (this._tabs.Count <= this._model.CurrentFocusTabIndex)
                {
                    this._model.CurrentFocusTabIndex = 0;
                }
                return this._tabs[this._model.CurrentFocusTabIndex];
            }
            set
            {
                var index = this._tabs.IndexOf(value);
                if (this._model.CurrentFocusTabIndex == index) return;
                this._model.CurrentFocusTabIndex = this._tabs.IndexOf(value);
                this._tabs.ForEach(item => item.UpdateFocus());
                this.RaisePropertyChanged();
            }
        }

        #region DragDrop Control

        private Livet.Commands.ViewModelCommand _dragDropStartCommand;

        public Livet.Commands.ViewModelCommand DragDropStartCommand
        {
            get
            {
                return this._dragDropStartCommand ??
                       (this._dragDropStartCommand = new Livet.Commands.ViewModelCommand(this.DragDropStart));
            }
        }

        public void DragDropStart()
        {
            this.Parent.StartDragDrop();
        }

        private Livet.Commands.ViewModelCommand _dragDropFinishCommand;

        public Livet.Commands.ViewModelCommand DragDropFinishCommand
        {
            get
            {
                return this._dragDropFinishCommand ??
                       (this._dragDropFinishCommand = new Livet.Commands.ViewModelCommand(this.DragDropFinish));
            }
        }

        public void DragDropFinish()
        {
            this.Parent.FinishDragDrop();
        }

        private bool _isDragDropping;
        public bool IsDragDropping
        {
            get { return this._isDragDropping; }
            set
            {
                this._isDragDropping = value;
                this.RaisePropertyChanged();
            }
        }

        private DropAcceptDescription _leftAccept;

        private DropAcceptDescription _rightAccept;

        public DropAcceptDescription LeftAcceptDescription
        {
            get
            {
                if (this._leftAccept == null)
                {
                    this._leftAccept = new DropAcceptDescription();
                    this._leftAccept.DragOver += this.AcceptTabViewModel;
                    this._leftAccept.DragDrop += e => this.DropCreateNewColumn(e, false);
                }
                return this._leftAccept;
            }
        }

        public DropAcceptDescription RightAcceptDescription
        {
            get
            {
                if (this._rightAccept == null)
                {
                    this._rightAccept = new DropAcceptDescription();
                    this._rightAccept.DragOver += this.AcceptTabViewModel;
                    this._rightAccept.DragDrop += e => this.DropCreateNewColumn(e, true);
                }
                return this._rightAccept;
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
            var curindex = TabManager.FindColumnIndex(this.Model);
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
            if (focusTargetIndex >= 0 && focusTargetIndex < this.Parent.Columns.Count)
            {
                this.Parent.Columns[focusTargetIndex].Focus();
            }
        }

        #endregion

        private DropAcceptDescription _description;
        public DropAcceptDescription DropAcceptDescription
        {
            get
            {
                if (this._description == null)
                {
                    this._description = new DropAcceptDescription();
                    this._description.DragOver += e =>
                    {
                        var data = e.Data.GetData(typeof(TabViewModel)) as TabViewModel;
                        e.Effects = data != null ? DragDropEffects.Move : DragDropEffects.None;
                    };
                    this._description.DragDrop += e =>
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
                            destColumnIndex = TabManager.FindColumnIndex(this.Model);
                            destTabIndex = TabManager.FindTabIndex(tvm.Model, destColumnIndex);
                        }
                        else if (cvm != null)
                        {
                            destColumnIndex = TabManager.FindColumnIndex(this.Model);
                            destTabIndex = this.Model.Tabs.Count;
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
                        if (this.Model != dataPreviousParent.Model && dataPreviousParent.Model.Tabs.Count > 0)
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
                        this.Model.CurrentFocusTabIndex = destTabIndex;
                        this.Focus();
                    };
                }
                return this._description;
            }
        }

        private void UpdateFocusFromModel(int newFocus)
        {
            DispatcherHolder.Enqueue(() =>
            {
                this._tabs.ForEach(item => item.UpdateFocus());
                this.RaisePropertyChanged(() => this.FocusedTab);
            });
        }

        public ColumnViewModel(MainAreaViewModel parent, ColumnModel model)
        {
            this._parent = parent;
            this._model = model;
            this.CompositeDisposable.Add(
                this._tabs = ViewModelHelperRx.CreateReadOnlyDispatcherCollectionRx(
                    model.Tabs,
                    _ => new TabViewModel(this, _),
                    DispatcherHelper.UIDispatcher));
            this.CompositeDisposable.Add(
                Observable.FromEvent(
                h => this._model.CurrentFocusTabChanged += h,
                h => this._model.CurrentFocusTabChanged -= h)
                .Select(_ => this._model.CurrentFocusTabIndex)
                .Subscribe(this.UpdateFocusFromModel));
            if (this._tabs.Count > 0)
            {
                this.FocusedTab = this._tabs[0];
            }
        }

        public bool IsFocused
        {
            get { return this._parent.FocusedColumn == this; }
        }

        internal void UpdateFocus()
        {
            this.RaisePropertyChanged(() => this.IsFocused);
        }

        public void Focus()
        {
            this._parent.FocusedColumn = this;
        }

        internal void CloseTab(TabViewModel tab)
        {
            this._parent.CloseTab(this, tab);
            this._parent.Columns.ForEach(cvm => cvm.RestoreLastClosedTabCommand.RaiseCanExecuteChanged());
        }

        #region CreateNewTabCommand
        private Livet.Commands.ViewModelCommand _createNewTabCommand;

        public Livet.Commands.ViewModelCommand CreateNewTabCommand
        {
            get
            {
                return this._createNewTabCommand ??
                       (this._createNewTabCommand = new Livet.Commands.ViewModelCommand(this.CreateNewTab));
            }
        }

        public void CreateNewTab()
        {
            var creating = TabModel.Create(String.Empty, null);
            MainWindowModel.ShowTabConfigure(creating)
                           .Subscribe(_ =>
                           {
                               if (String.IsNullOrEmpty(creating.Name) && creating.FilterQuery == null) return;
                               this.Model.CreateTab(creating);
                           });
        }
        #endregion

        #region RestoreLastClosedTabCommand
        private Livet.Commands.ViewModelCommand _restoreLastClosedTabCommand;

        public Livet.Commands.ViewModelCommand RestoreLastClosedTabCommand
        {
            get
            {
                return this._restoreLastClosedTabCommand ??
                       (this._restoreLastClosedTabCommand =
                        new Livet.Commands.ViewModelCommand(this.RestoreLastClosedTab,
                                                            this.CanRestoreClosedTab));
            }
        }

        public bool CanRestoreClosedTab()
        {
            return TabManager.CanReviveTab;
        }

        public void RestoreLastClosedTab()
        {
            this.Focus();
            if (TabManager.CanReviveTab)
            {
                TabManager.ReviveTab();
            }
            this._parent.Columns.ForEach(cvm => cvm.RestoreLastClosedTabCommand.RaiseCanExecuteChanged());
        }
        #endregion
    }
}
