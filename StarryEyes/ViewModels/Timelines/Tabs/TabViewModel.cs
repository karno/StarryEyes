using System;
using System.Collections.Generic;
using System.Linq;
using Livet.Messaging;
using StarryEyes.Annotations;
using StarryEyes.Filters.Parsing;
using StarryEyes.Models;
using StarryEyes.Models.Timelines.Tabs;

namespace StarryEyes.ViewModels.Timelines.Tabs
{
    public class TabViewModel : TimelineViewModelBase
    {
        private readonly ColumnViewModel _parent;
        private readonly TabModel _model;

        private int _unreadCount;

        /// <summary>
        /// design time support method.
        /// </summary>
        [UsedImplicitly]
        public TabViewModel()
            : base(null)
        {
        }

        public TabViewModel(ColumnViewModel parent, TabModel model)
            : base(model)
        {
            this._parent = parent;
            this._model = model;
            this._model.OnNewStatusArrival += _ => this.UnreadCount++;
            this._model.BindingAccountsChanged += () => this.RaisePropertyChanged(() => this.CurrentAccounts);
            this._model.FocusRequired += this.SetFocus;
        }

        public TabModel Model
        {
            get { return this._model; }
        }

        public string Name
        {
            get { return this._model.Name; }
            set
            {
                this._model.Name = value;
                this.RaisePropertyChanged();
            }
        }

        public ColumnViewModel Parent
        {
            get { return this._parent; }
        }

        public bool IsFocused
        {
            get { return this._parent.FocusedTab == this; }
        }

        public void SetFocus()
        {
            this.Messenger.Raise(new InteractionMessage("SetFocus"));
        }

        internal void UpdateFocus()
        {
            if (this.IsFocused)
            {
                this.UnreadCount = 0;
            }
            this.RaisePropertyChanged(() => this.IsFocused);
        }

        protected override IEnumerable<long> CurrentAccounts
        {
            get { return this._model.BindingAccounts; }
        }

        public int UnreadCount
        {
            get { return this._unreadCount; }
            set
            {
                var newValue = this.IsFocused || !this.Model.IsShowUnreadCounts ? 0 : value;
                if (this._unreadCount == newValue) return;
                this._unreadCount = newValue;
                this.RaisePropertyChanged();
                this.RaisePropertyChanged(() => this.IsUnreadExisted);
            }
        }

        public bool IsUnreadExisted
        {
            get { return this.UnreadCount > 0; }
        }

        protected override void ReadMore(long id)
        {
            base.ReadMore(id);
            this.ReadMoreFromWeb(id);
        }

        public void ReadMoreFromWeb(long? id)
        {
            // TODO: implement?
            /*
                TimelineModel.IsSuppressTimelineTrimming = true;
                IsLoading = true;
                Model.ReceiveTimelines(id)
                     .Finally(() => IsLoading = false)
                     .OnErrorResumeNext(Observable.Empty<Unit>())
                     .Subscribe();
            */
        }

        public override void GotFocus()
        {
            this._parent.FocusedTab = this;
            this._parent.Focus();
        }

        #region EditTabCommand
        private Livet.Commands.ViewModelCommand _editTabCommand;

        public Livet.Commands.ViewModelCommand EditTabCommand
        {
            get { return this._editTabCommand ?? (this._editTabCommand = new Livet.Commands.ViewModelCommand(this.EditTab)); }
        }

        public void EditTab()
        {
            MainWindowModel.ShowTabConfigure(this.Model)
                           .Subscribe(_ => this.RaisePropertyChanged(() => this.Name));
        }
        #endregion

        #region CopyTabCommand
        private Livet.Commands.ViewModelCommand _copyTabCommand;

        public Livet.Commands.ViewModelCommand CopyTabCommand
        {
            get { return this._copyTabCommand ?? (this._copyTabCommand = new Livet.Commands.ViewModelCommand(this.CopyTab)); }
        }

        public void CopyTab()
        {
            var model = new TabModel
            {
                Name = this.Name,
                FilterQuery = this.Model.FilterQuery != null ? QueryCompiler.Compile(this.Model.FilterQuery.ToQuery()) : null,
                BindingHashtags = this.Model.BindingHashtags.ToArray(),
                IsNotifyNewArrivals = this.Model.IsNotifyNewArrivals,
                IsShowUnreadCounts = this.Model.IsShowUnreadCounts,
                NotifySoundSource = this.Model.NotifySoundSource
            };
            this.Model.BindingAccounts.ForEach(id => model.BindingAccounts.Add(id));
            this.Parent.Model.CreateTab(model);
        }
        #endregion

        #region CloseTabCommand
        private Livet.Commands.ViewModelCommand _closeTabCommand;

        public Livet.Commands.ViewModelCommand CloseTabCommand
        {
            get { return this._closeTabCommand ?? (this._closeTabCommand = new Livet.Commands.ViewModelCommand(this.CloseTab)); }
        }

        public void CloseTab()
        {
            this.Parent.CloseTab(this);
        }
        #endregion
    }
}
