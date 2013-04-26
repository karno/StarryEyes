using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Windows.Threading;
using Livet.Messaging;
using StarryEyes.Annotations;
using StarryEyes.Breezy.DataModel;
using StarryEyes.Models;
using StarryEyes.Models.Tab;
using StarryEyes.Settings;

namespace StarryEyes.ViewModels.WindowParts.Timelines
{
    /// <summary>
    ///     タブにバインドされるViewModelを表現します。
    /// </summary>
    public sealed class TabViewModel : TimelineViewModelBase
    {
        private readonly ColumnViewModel _parent;
        public ColumnViewModel Parent
        {
            get { return _parent; }
        }

        private readonly TabModel _model;
        public TabModel Model
        {
            get { return _model; }
        }

        private int _unreadCount;

        private int _refreshCount;

        /// <summary>
        ///     for design time support.
        /// </summary>
        [UsedImplicitly]
        public TabViewModel()
        {
            _model = null;
        }

        public TabViewModel(ColumnViewModel parent, TabModel tabModel)
        {
            _parent = parent;
            _model = tabModel;
            tabModel.Activate();
            BindTimeline();
            CompositeDisposable.Add(
                Observable.FromEvent<ScrollLockStrategy>(
                    handler => Setting.ScrollLockStrategy.OnValueChanged += handler,
                    handler => Setting.ScrollLockStrategy.OnValueChanged -= handler)
                          .Subscribe(_ =>
                          {
                              RaisePropertyChanged(() => IsScrollLock);
                              RaisePropertyChanged(() => IsScrollLockExplicitEnabled);
                          }));
            CompositeDisposable.Add(() => Model.Deactivate());
            if (Model.FilterQuery.PredicateTreeRoot != null)
            {
                CompositeDisposable.Add(
                    Observable.FromEvent(
                        h => Model.FilterQuery.OnInvalidateRequired += h,
                        h => Model.FilterQuery.OnInvalidateRequired -= h)
                              .Subscribe(_ =>
                              {
                                  var count = Interlocked.Increment(ref _refreshCount);
                                  Observable.Timer(TimeSpan.FromSeconds(3))
                                            .Where(__ => Interlocked.CompareExchange(ref _refreshCount, 0, count) == count)
                                            .Subscribe(__ =>
                                            {
                                                System.Diagnostics.Debug.WriteLine("* invalidate executed: " + Name);
                                                // regenerate filter query
                                                Model.RefreshEvaluator();
                                                Model.InvalidateCollection();
                                                BindTimeline();
                                            });
                              }));
            }
            CompositeDisposable.Add(
                Observable.FromEvent(
                    h => tabModel.OnBindingAccountIdsChanged += h,
                    h => tabModel.OnBindingAccountIdsChanged -= h)
                          .Subscribe(
                              _ => DispatcherHolder.Enqueue(
                                  () => Timeline.ForEach(t => t.BindingAccounts = Model.BindingAccountIds),
                                  DispatcherPriority.Background)));
            CompositeDisposable.Add(
                Observable.FromEvent<bool>(
                    h => tabModel.OnConfigurationUpdated += h,
                    h => tabModel.OnConfigurationUpdated -= h)
                          .Subscribe(timelineModelRegenerated =>
                          {
                              RaisePropertyChanged(() => Name);
                              RaisePropertyChanged(() => UnreadCount);
                              if (timelineModelRegenerated)
                              {
                                  BindTimeline();
                              }
                          }));
            CompositeDisposable.Add(
                Observable.FromEvent(
                h => tabModel.OnSetPhysicalFocusRequired += h,
                h => tabModel.OnSetPhysicalFocusRequired -= h)
                .Subscribe(_ => this.Messenger.Raise(new InteractionMessage("SetPhysicalFocus"))));
        }

        public override void GotPhysicalFocus()
        {
            _parent.Focus();
        }

        private void BindTimeline()
        {
            CompositeDisposable.Add(
                Observable.FromEvent<TwitterStatus>(
                    h => Model.Timeline.OnNewStatusArrival += h,
                    h =>
                    {
                        if (Model.Timeline != null)
                            Model.Timeline.OnNewStatusArrival -= h;
                    })
                          .Subscribe(_ => UnreadCount++));
            // invalidate cache
            ReInitializeTimeline();
            IsLoading = true;

            Observable.Start(() => Model.Timeline.ReadMore(null, true))
                      .Merge(Observable.Start(() => Model.ReceiveTimelines(null)))
                      .SelectMany(_ => _)
                      .Finally(() => IsLoading = false)
                      .Subscribe();
            if (UnreadCount > 0)
            {
                UnreadCount = 0;
            }
        }

        public string Name
        {
            get { return Model.Name; }
            set
            {
                if (Model.Name == value) return;
                Model.Name = value;
                RaisePropertyChanged();
            }
        }

        public bool IsFocused
        {
            get { return Parent.FocusedTab == this; }
        }

        internal void UpdateFocus()
        {
            if (IsFocused)
            {
                UnreadCount = 0;
            }
            this.RaisePropertyChanged(() => IsFocused);
        }

        protected override TimelineModel TimelineModel
        {
            get { return Model.Timeline; }
        }

        protected override IEnumerable<long> CurrentAccounts
        {
            get { return Model.BindingAccountIds; }
        }

        public int UnreadCount
        {
            get { return _unreadCount; }
            set
            {
                int newValue = IsFocused || !Model.IsShowUnreadCounts ? 0 : value;
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

        public void Focus()
        {
            Parent.FocusedTab = this;
            // propagate focus
            Parent.Focus();
        }

        protected override void ReadMore(long id)
        {
            base.ReadMore(id);
            ReadMoreFromWeb(id);
        }

        public void ReadMoreFromWeb(long? id)
        {
            TimelineModel.IsSuppressTimelineTrimming = true;
            IsLoading = true;
            Model.ReceiveTimelines(id)
                 .Finally(() => IsLoading = false)
                 .OnErrorResumeNext(Observable.Empty<Unit>())
                 .Subscribe();
        }

        #region EditTabCommand
        private Livet.Commands.ViewModelCommand _editTabCommand;

        public Livet.Commands.ViewModelCommand EditTabCommand
        {
            get { return _editTabCommand ?? (_editTabCommand = new Livet.Commands.ViewModelCommand(EditTab)); }
        }

        public void EditTab()
        {
            MainWindowModel.ShowTabConfigure(this.Model);
        }
        #endregion

        #region CopyTabCommand
        private Livet.Commands.ViewModelCommand _copyTabCommand;

        public Livet.Commands.ViewModelCommand CopyTabCommand
        {
            get { return _copyTabCommand ?? (_copyTabCommand = new Livet.Commands.ViewModelCommand(CopyTab)); }
        }

        public void CopyTab()
        {
            Parent.Model.CreateTab(
                new TabModel(this.Name + "_",
                             this.Model.FilterQueryString));
        }
        #endregion

        #region CloseTabCommand
        private Livet.Commands.ViewModelCommand _closeTabCommand;

        public Livet.Commands.ViewModelCommand CloseTabCommand
        {
            get { return _closeTabCommand ?? (_closeTabCommand = new Livet.Commands.ViewModelCommand(CloseTab)); }
        }

        public void CloseTab()
        {
            Parent.CloseTab(this);
        }
        #endregion
    }
}