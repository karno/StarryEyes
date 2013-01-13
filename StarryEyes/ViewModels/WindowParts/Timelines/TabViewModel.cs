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
using StarryEyes.Models.Tab;
using StarryEyes.Settings;

namespace StarryEyes.ViewModels.WindowParts.Timelines
{
    /// <summary>
    ///     タブにバインドされるViewModelを表現します。
    /// </summary>
    public class TabViewModel : ViewModel
    {
        private readonly ColumnViewModel _owner;
        private readonly ObservableCollection<StatusViewModel> _timeline;
        private bool _isLoading;
        private bool _isMouseOver;
        private bool _isScrollLockExplicit;
        private bool _isSelected;

        private TabModel _model;
        private int _unreadCount;
        private bool _isScrollInTop;
        private bool _isScrollInBottom;

        /// <summary>
        ///     for design time support.
        /// </summary>
        public TabViewModel()
        {
        }

        public TabViewModel(ColumnViewModel owner, TabModel tabModel)
        {
            _timeline = new ObservableCollection<StatusViewModel>();
            _owner = owner;
            _model = tabModel;
            tabModel.Activate();
            DispatcherHolder.Push(InitializeCollection);
            CompositeDisposable.Add(
                Observable.FromEvent(
                    _ => Model.Timeline.OnNewStatusArrived += _,
                    _ => Model.Timeline.OnNewStatusArrived -= _)
                          .Subscribe(_ => UnreadCount++));
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
            IsLoading = true;
            Observable.Defer(
                () =>
                Observable.Start(() => Model.Timeline.ReadMore(null, true))
                          .Merge(Observable.Start(() => Model.ReceiveTimelines(null))))
                      .SelectMany(_ => _)
                      .Finally(() => IsLoading = false)
                      .Subscribe();
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

        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (_isSelected == value) return;
                _isSelected = value;
                if (value)
                {
                    UnreadCount = 0;
                }
                RaisePropertyChanged();
            }
        }

        public ObservableCollection<StatusViewModel> Timeline
        {
            get { return _timeline; }
        }

        public int UnreadCount
        {
            get { return _unreadCount; }
            set
            {
                var newValue = IsSelected ? 0 : value;
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

        private readonly object _collectionLock = new object();

        private void InitializeCollection()
        {
            lock (_collectionLock)
            {
                bool activated = false;
                CompositeDisposable.Add(
                    new CollectionChangedEventListener(
                        Model.Timeline.Statuses,
                        (sender, e) =>
                        {
                            if (activated)
                            {
                                DispatcherHolder.Push(
                                    () => ReflectCollectionChanged(e));
                            }
                        }));
                var collection = Model.Timeline.Statuses.ToArray();
                activated = true;
                collection
                          .Select(GenerateStatusViewModel)
                          .ForEach(_timeline.Add);
            }
        }

        private void ReflectCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            lock (_collectionLock)
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        _timeline.Insert(e.NewStartingIndex, GenerateStatusViewModel((TwitterStatus)e.NewItems[0]));
                        break;
                    case NotifyCollectionChangedAction.Move:
                        _timeline.Move(e.OldStartingIndex, e.NewStartingIndex);
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        StatusViewModel removal = _timeline[e.OldStartingIndex];
                        _timeline.RemoveAt(e.OldStartingIndex);
                        removal.Dispose();
                        break;
                    case NotifyCollectionChangedAction.Reset:
                        StatusViewModel[] cache = _timeline.ToArray();
                        _timeline.Clear();
                        cache.ToObservable()
                             .ObserveOn(TaskPoolScheduler.Default)
                             .Subscribe(_ => _.Dispose());
                        TwitterStatus[] collection = Model.Timeline.Statuses.ToArray();
                        collection
                            .Select(GenerateStatusViewModel)
                            .ForEach(_timeline.Add);
                        break;
                    default:
                        throw new ArgumentException();
                }
            }
        }

        private StatusViewModel GenerateStatusViewModel(TwitterStatus status)
        {
            return new StatusViewModel(this, status, Model.BindingAccountIds);
        }

        public void Select()
        {
            _owner.SetSelected(Model);
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
                var previous = _focusedStatus;
                _focusedStatus = value;
                if (previous != null)
                    previous.RaiseFocusedChanged();
                if (_focusedStatus != null)
                    _focusedStatus.RaiseFocusedChanged();
            }
        }

        public void FocusUp()
        {
            if (this.FocusedStatus == null) return;
            var index = Timeline.IndexOf(this.FocusedStatus) - 1;
            this.FocusedStatus = index < 0 ? null : Timeline[index];
        }

        public void FocusDown()
        {
            if (this.FocusedStatus == null)
            {
                FocusTop();
                return;
            }
            var index = Timeline.IndexOf(this.FocusedStatus) + 1;
            if (index >= Timeline.Count) return;
            this.FocusedStatus = Timeline[index];
        }

        public void FocusTop()
        {
            if (Timeline.Count == 0) return;
            this.FocusedStatus = Timeline[0];
        }

        public void FocusBottom()
        {
            if (Timeline.Count == 0) return;
            this.FocusedStatus = Timeline[Timeline.Count - 1];
        }
        #endregion
    }
}