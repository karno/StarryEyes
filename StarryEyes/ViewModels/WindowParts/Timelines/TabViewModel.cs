using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows.Threading;
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
        private readonly ColumnViewModel _owner;

        private TabModel _model;
        private int _unreadCount;

        /// <summary>
        ///     for design time support.
        /// </summary>
        public TabViewModel()
        {
        }

        public TabViewModel(ColumnViewModel owner, TabModel tabModel)
        {
            _owner = owner;
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
                UnreadCount = 0;
        }

        public TabModel Model
        {
            get { return _model; }
            set { _model = value; }
        }

        public string Name
        {
            get { return _model.Name; }
            set
            {
                if (_model.Name == value) return;
                _model.Name = value;
                RaisePropertyChanged();
            }
        }

        public bool IsFocused
        {
            get { return _owner.Focused == this; }
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
            get { return _model.Timeline; }
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
            _owner.Focused = this;
        }

        public override void ReadMore(long id)
        {
            base.ReadMore(id);
            ReadMoreFromWeb(id);
        }

        public void ReadMoreFromWeb(long? id)
        {
            IsSuppressTimelineAutoTrim = true;
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
    }
}