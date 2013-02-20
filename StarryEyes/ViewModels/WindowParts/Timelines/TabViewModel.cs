using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Livet;
using Livet.EventListeners;
using StarryEyes.Breezy.DataModel;
using StarryEyes.Models;
using StarryEyes.Models.Tab;
using StarryEyes.Settings;

namespace StarryEyes.ViewModels.WindowParts.Timelines
{
    /// <summary>
    ///     タブにバインドされるViewModelを表現します。
    /// </summary>
    public class TabViewModel : ViewModel
    {
        private readonly object _collectionLock = new object();
        private readonly ColumnViewModel _owner;
        private ObservableCollection<StatusViewModel> _timeline;
        private bool _isLoading;
        private bool _isMouseOver;
        private bool _isScrollInBottom;
        private bool _isScrollInTop;
        private bool _isScrollLockExplicit;

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
                              _ =>
                              DispatcherHolder.Enqueue(
                                  () => Timeline.ForEach(t => t.BindingAccounts = Model.BindingAccountIds))));
            CompositeDisposable.Add(
                Observable.FromEvent<bool>(
                    h => tabModel.OnConfigurationUpdated += h,
                    h => tabModel.OnConfigurationUpdated -= h)
                          .Subscribe(isQueryUpdated =>
                          {
                              System.Diagnostics.Debug.WriteLine("Configuration updated.");
                              RaisePropertyChanged(() => Name);
                              RaisePropertyChanged(() => UnreadCount);
                              if (isQueryUpdated)
                              {
                                  BindTimeline();
                              }
                          }));
        }

        private void BindTimeline()
        {
            // invalidate cache
            System.Diagnostics.Debug.WriteLine("Re-bind cache.");
            _timeline = null;

            CompositeDisposable.Add(
                Observable.FromEvent<TwitterStatus>(
                    h => Model.Timeline.OnNewStatusArrival += h,
                    h =>
                    {
                        if (Model.Timeline != null)
                            Model.Timeline.OnNewStatusArrival -= h;
                    })
                          .Subscribe(_ => UnreadCount++));
            IsLoading = true;
            Observable.Start(() => Model.Timeline.ReadMore(null, true))
                      .Merge(Observable.Start(() => Model.ReceiveTimelines(null)))
                      .SelectMany(_ => _)
                      .Finally(() => IsLoading = false)
                      .Subscribe();
            if (UnreadCount > 0)
                UnreadCount = 0;
            RaisePropertyChanged(() => Timeline);
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

        public ObservableCollection<StatusViewModel> Timeline
        {
            get
            {
                if (_timeline == null)
                {
                    var ctl = _timeline = new ObservableCollection<StatusViewModel>();
                    DispatcherHolder.Enqueue(() => InitializeCollection(ctl));
                }
                return _timeline;
            }
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

        public bool IsLoading
        {
            get { return _isLoading; }
            set
            {
                if (_isLoading == value) return;
                _isLoading = value;
                RaisePropertyChanged();
            }
        }

        public bool IsMouseOver
        {
            get { return _isMouseOver; }
            set
            {
                if (_isMouseOver == value) return;
                _isMouseOver = value;
                RaisePropertyChanged();
                RaisePropertyChanged(() => IsScrollLock);
            }
        }

        public bool IsScrollInTop
        {
            get { return _isScrollInTop; }
            set
            {
                if (_isScrollInTop == value) return;
                _isScrollInTop = value;
                RaisePropertyChanged();
                RaisePropertyChanged(() => IsScrollLock);
            }
        }

        public bool IsScrollInBottom
        {
            get { return _isScrollInBottom; }
            set
            {
                if (_isScrollInBottom == value) return;
                _isScrollInBottom = value;
                RaisePropertyChanged();
                RaisePropertyChanged(() => IsScrollLock);
            }
        }

        public bool IsScrollLockExplicit
        {
            get { return _isScrollLockExplicit; }
            set
            {
                if (_isScrollLockExplicit == value) return;
                _isScrollLockExplicit = value;
                RaisePropertyChanged();
                RaisePropertyChanged(() => IsScrollLock);
            }
        }

        public bool IsScrollLockExplicitEnabled
        {
            get { return Setting.ScrollLockStrategy.Value == ScrollLockStrategy.Explicit; }
        }

        public bool IsScrollLock
        {
            get
            {
                switch (Setting.ScrollLockStrategy.Value)
                {
                    case ScrollLockStrategy.None:
                        return false;
                    case ScrollLockStrategy.Always:
                        return true;
                    case ScrollLockStrategy.WhenMouseOver:
                        return IsMouseOver;
                    case ScrollLockStrategy.WhenScrolled:
                        return !_isScrollInTop;
                    case ScrollLockStrategy.Explicit:
                        return IsScrollLockExplicit;
                    default:
                        return false;
                }
            }
        }

        private volatile bool _isCollectionAddEnabled;
        private void InitializeCollection(ObservableCollection<StatusViewModel> ctl)
        {
            lock (_collectionLock)
            {
                CompositeDisposable.Add(
                    new CollectionChangedEventListener(
                        Model.Timeline.Statuses,
                        (sender, e) =>
                        {
                            if (_isCollectionAddEnabled)
                            {
                                DispatcherHolder.Enqueue(
                                    () => ReflectCollectionChanged(e, ctl));
                            }
                        }));
                TwitterStatus[] collection = Model.Timeline.Statuses.ToArray();
                _isCollectionAddEnabled = true;
                collection
                    .Select(GenerateStatusViewModel)
                    .ForEach(ctl.Add);
            }
        }

        private void ReflectCollectionChanged(NotifyCollectionChangedEventArgs e, ObservableCollection<StatusViewModel> ctl)
        {
            lock (_collectionLock)
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        ctl.Insert(e.NewStartingIndex, GenerateStatusViewModel((TwitterStatus)e.NewItems[0]));
                        break;
                    case NotifyCollectionChangedAction.Move:
                        ctl.Move(e.OldStartingIndex, e.NewStartingIndex);
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        StatusViewModel removal = ctl[e.OldStartingIndex];
                        ctl.RemoveAt(e.OldStartingIndex);
                        removal.Dispose();
                        break;
                    case NotifyCollectionChangedAction.Reset:
                        Rebind(ctl);
                        break;
                    default:
                        throw new ArgumentException();
                }
            }
        }

        public void Rebind(ObservableCollection<StatusViewModel> ctl)
        {
            lock (_collectionLock)
            {
                _isCollectionAddEnabled = false;
                StatusViewModel[] cache = ctl.ToArray();
                ctl.Clear();
                cache.ToObservable()
                     .ObserveOn(TaskPoolScheduler.Default)
                     .Subscribe(_ => _.Dispose());
                TwitterStatus[] collection = Model.Timeline.Statuses.ToArray();
                _isCollectionAddEnabled = true;
                collection
                    .Select(GenerateStatusViewModel)
                    .ForEach(ctl.Add);
            }
        }

        private StatusViewModel GenerateStatusViewModel(TwitterStatus status)
        {
            return new StatusViewModel(this, status, Model.BindingAccountIds);
        }

        public void Focus()
        {
            _owner.Focused = this;
        }

        public void ReadMore()
        {
            ReadMore(_model.Timeline.Statuses.Select(_ => _.Id).Min());
        }

        public void ReadMore(long id)
        {
            IsSuppressTimelineAutoTrim = true;
            IsLoading = true;
            Model.Timeline.ReadMore(id)
                 .Finally(() => IsLoading = false)
                 .OnErrorResumeNext(Observable.Empty<Unit>())
                 .Subscribe();
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

        #region Call by code-behind

        public bool IsSuppressTimelineAutoTrim
        {
            get { return Model.Timeline.IsSuppressTimelineTrimming; }
            set { Model.Timeline.IsSuppressTimelineTrimming = value; }
        }

        #endregion

        #region Selection Control

        public bool IsSelectedStatusExisted
        {
            get { return Timeline.FirstOrDefault(_ => _.IsSelected) != null; }
        }

        public void OnSelectionUpdated()
        {
            RaisePropertyChanged(() => IsSelectedStatusExisted);
            // TODO: Impl
        }

        public void DeselectAll()
        {
            Timeline.ForEach(s => s.IsSelected = false);
        }

        #endregion

        #region Focus Control

        private StatusViewModel _focusedStatus;

        public StatusViewModel FocusedStatus
        {
            get { return _focusedStatus; }
            set
            {
                StatusViewModel previous = _focusedStatus;
                _focusedStatus = value;
                if (previous != null)
                    previous.RaiseFocusedChanged();
                if (_focusedStatus != null)
                    _focusedStatus.RaiseFocusedChanged();
            }
        }

        public void FocusUp()
        {
            if (FocusedStatus == null) return;
            int index = Timeline.IndexOf(FocusedStatus) - 1;
            FocusedStatus = index < 0 ? null : Timeline[index];
        }

        public void FocusDown()
        {
            if (FocusedStatus == null)
            {
                FocusTop();
                return;
            }
            int index = Timeline.IndexOf(FocusedStatus) + 1;
            if (index >= Timeline.Count) return;
            FocusedStatus = Timeline[index];
        }

        public void FocusTop()
        {
            if (Timeline.Count == 0) return;
            FocusedStatus = Timeline[0];
        }

        public void FocusBottom()
        {
            if (Timeline.Count == 0) return;
            FocusedStatus = Timeline[Timeline.Count - 1];
        }

        #endregion
    }
}