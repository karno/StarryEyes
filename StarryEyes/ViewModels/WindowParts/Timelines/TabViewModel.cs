using System;
using System.Collections.Generic;
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
        private double _scrollIndex;
        private int _unreadCount;

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
                Observable.Start(() => Model.Timeline.ReadMore(null))
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
                _model.Name = value;
                RaisePropertyChanged();
            }
        }

        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
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
                _unreadCount = IsSelected ? 0 : value;
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
                _isLoading = value;
                RaisePropertyChanged();
            }
        }

        public bool IsMouseOver
        {
            get { return _isMouseOver; }
            set
            {
                _isMouseOver = value;
                RaisePropertyChanged();
                RaisePropertyChanged(() => IsScrollLock);
            }
        }

        public double ScrollIndex
        {
            get { return _scrollIndex; }
            set
            {
                _scrollIndex = value;
                RaisePropertyChanged();
                RaisePropertyChanged(() => IsScrollLock);
            }
        }

        public bool IsScrollLockExplicit
        {
            get { return _isScrollLockExplicit; }
            set
            {
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
                        return _scrollIndex > 0;
                    case ScrollLockStrategy.Explicit:
                        return IsScrollLockExplicit;
                    default:
                        return false;
                }
            }
        }

        private void InitializeCollection()
        {
            List<TwitterStatus> collection = Model.Timeline.Statuses.ToList();
            DispatcherHolder.Push(
                () => collection
                          .Select(GenerateStatusViewModel)
                          .ForEach(_timeline.Add));
            CompositeDisposable.Add(
                new CollectionChangedEventListener(
                    Model.Timeline.Statuses,
                    (sender, e) =>
                    DispatcherHolder.Push(
                        () => ReflectCollectionChanged(e))));
        }

        private void ReflectCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    _timeline.Insert(e.NewStartingIndex, GenerateStatusViewModel((TwitterStatus) e.NewItems[0]));
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
    }
}