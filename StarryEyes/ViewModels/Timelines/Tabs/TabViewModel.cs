using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Threading;
using Cadena.Data;
using JetBrains.Annotations;
using Livet;
using Livet.EventListeners;
using Livet.Messaging;
using StarryEyes.Filters;
using StarryEyes.Filters.Parsing;
using StarryEyes.Globalization.Filters;
using StarryEyes.Models;
using StarryEyes.Models.Backstages.NotificationEvents;
using StarryEyes.Models.Timelines.Tabs;

namespace StarryEyes.ViewModels.Timelines.Tabs
{
    public class TabViewModel : TimelineViewModelBase
    {
        private readonly ColumnViewModel _parent;

        private readonly TabModel _model;

        private int _unreadCount;

        public TabModel Model
        {
            get { return _model; }
        }

        public string Name
        {
            get { return _model.Name; }
            set
            {
                _model.Name = value;
                RaisePropertyChanged();
            }
        }

        public ColumnViewModel Parent
        {
            get { return _parent; }
        }

        public bool IsFocused
        {
            get { return _parent.FocusedTab == this; }
        }

        protected override IEnumerable<long> CurrentAccounts
        {
            get { return _model.BindingAccounts; }
        }

        public int UnreadCount
        {
            get { return _unreadCount; }
            set
            {
                var newValue = IsFocused || !Model.ShowUnreadCounts ? 0 : value;
                if (_unreadCount == newValue) return;
                _unreadCount = newValue;
                RaisePropertyChanged();
                RaisePropertyChanged(() => IsUnreadExisted);
            }
        }

        public bool IsUnreadExisted
        {
            get { return UnreadCount > 0; }
        }

        public bool IsNotifyNewArrivals
        {
            get { return _model.NotifyNewArrivals; }
            set
            {
                if (_model.NotifyNewArrivals == value) return;
                _model.NotifyNewArrivals = value;
                RaisePropertyChanged(() => IsNotifyNewArrivals);
                TabManager.Save();
            }
        }

        public TabViewModel(ColumnViewModel parent, TabModel model)
            : base(model)
        {
            _parent = parent;
            _model = model;
            CompositeDisposable.Add(
                new EventListener<Action<TwitterStatus>>(
                    h => _model.OnNewStatusArrival += h,
                    h => model.OnNewStatusArrival -= h,
                    _ => UnreadCount++));
            CompositeDisposable.Add(
                new EventListener<Action>(
                    h => _model.BindingAccountsChanged += h,
                    h => _model.BindingAccountsChanged -= h,
                    () => RaisePropertyChanged(() => CurrentAccounts)));
            CompositeDisposable.Add(
                new EventListener<Action>(
                    h => _model.FocusRequired += h,
                    h => _model.FocusRequired -= h,
                    SetFocus));
            model.IsActivated = true;
        }

        public void SetFocus()
        {
            _parent.FocusedTab = this;
            _parent.Focus();
            Messenger.RaiseSafe(() => new InteractionMessage("SetFocus"));
        }

        internal void UpdateFocus()
        {
            if (IsFocused)
            {
                UnreadCount = 0;
            }
            RaisePropertyChanged(() => IsFocused);
        }

        public override void GotFocus()
        {
            _parent.FocusedTab = this;
            _parent.Focus();
            // wait for completion of creating view
            DispatcherHelper.UIDispatcher.InvokeAsync(SetFocus, DispatcherPriority.Input);
        }

        #region EditTabCommand

        private Livet.Commands.ViewModelCommand _editTabCommand;

        [UsedImplicitly]
        public Livet.Commands.ViewModelCommand EditTabCommand
        {
            get
            {
                return _editTabCommand ??
                       (_editTabCommand = new Livet.Commands.ViewModelCommand(EditTab));
            }
        }

        public void EditTab()
        {
            MainWindowModel.ShowTabConfigure(Model)
                           .Subscribe(_ =>
                           {
                               RaisePropertyChanged(() => Name);
                               RaisePropertyChanged(() => IsNotifyNewArrivals);
                           });
        }

        #endregion EditTabCommand

        #region CopyTabCommand

        private Livet.Commands.ViewModelCommand _copyTabCommand;

        [UsedImplicitly]
        public Livet.Commands.ViewModelCommand CopyTabCommand
        {
            get
            {
                return _copyTabCommand ??
                       (_copyTabCommand = new Livet.Commands.ViewModelCommand(CopyTab));
            }
        }

        public void CopyTab()
        {
            try
            {
                var model = new TabModel
                {
                    Name = Name,
                    FilterQuery = Model.FilterQuery != null
                        ? QueryCompiler.Compile(Model.FilterQuery.ToQuery())
                        : null,
                    RawQueryString = Model.RawQueryString,
                    BindingHashtags = Model.BindingHashtags.ToArray(),
                    NotifyNewArrivals = Model.NotifyNewArrivals,
                    ShowUnreadCounts = Model.ShowUnreadCounts,
                    NotifySoundSource = Model.NotifySoundSource
                };
                Model.BindingAccounts.ForEach(id => model.BindingAccounts.Add(id));
                Parent.Model.CreateTab(model);
            }
            catch (FilterQueryException fqex)
            {
                BackstageModel.RegisterEvent(
                    new OperationFailedEvent(QueryCompilerResources.QueryCompileFailed, fqex));
            }
        }

        #endregion CopyTabCommand

        #region CloseTabCommand

        private Livet.Commands.ViewModelCommand _closeTabCommand;

        [UsedImplicitly]
        public Livet.Commands.ViewModelCommand CloseTabCommand => _closeTabCommand ?? (_closeTabCommand =
                                                                      new Livet.Commands.ViewModelCommand(CloseTab));

        public void CloseTab()
        {
            Parent.CloseTab(this);
        }

        #endregion CloseTabCommand
    }
}