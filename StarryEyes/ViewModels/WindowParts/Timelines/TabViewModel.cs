using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Livet;
using Livet.EventListeners;
using StarryEyes.Breezy.DataModel;
using StarryEyes.Models.Tab;
using StarryEyes.Settings;

namespace StarryEyes.ViewModels.WindowParts.Timelines
{
    /// <summary>
    /// タブにバインドされるViewModelを表現します。
    /// </summary>
    public class TabViewModel : ViewModel
    {
        private ColumnViewModel _owner;

        private TabModel _model;
        public TabModel Model
        {
            get { return _model; }
            set { _model = value; }
        }

        /// <summary>
        /// for design time support.
        /// </summary>
        public TabViewModel() { }
        public TabViewModel(ColumnViewModel owner, TabModel tabModel)
        {
            this._timeline = new ObservableCollection<StatusViewModel>();
            this._owner = owner;
            this._model = tabModel;
            if (tabModel.IsActivated)
            {
                Initialize();
            }
            else
            {
                this.IsLoading = true;
                Observable.Start(() => tabModel.Activate())
                    .SelectMany(_ => _)
                    .ObserveOnDispatcher()
                    .Subscribe(_ => { }, Initialize);
            }
        }

        private void Initialize()
        {
            DispatcherHolder.Push(InitializeCollection);
            this.CompositeDisposable.Add(Observable.FromEvent(
                _ => Model.Timeline.OnNewStatusArrived += _,
                _ => Model.Timeline.OnNewStatusArrived -= _)
                .Subscribe(_ => UnreadCount++));
            this.CompositeDisposable.Add(Observable.FromEvent<ScrollLockStrategy>(
                handler => Setting.ScrollLockStrategy.OnValueChanged += handler,
                handler => Setting.ScrollLockStrategy.OnValueChanged -= handler)
                .Subscribe(_ =>
                {
                    RaisePropertyChanged(() => IsScrollLock);
                    RaisePropertyChanged(() => IsScrollLockExplicitEnabled);
                }));
            this.CompositeDisposable.Add(() => Model.Deactivate());
            this.IsLoading = false;
        }

        private void InitializeCollection()
        {
            var collection = Model.Timeline.Statuses.ToArray();
            this.CompositeDisposable.Add(new CollectionChangedEventListener(Model.Timeline.Statuses,
                (sender, e) => DispatcherHolder.Push(() => ReflectCollectionChanged(e))));
            collection
                .Select(GenerateStatusViewModel)
                .ForEach(_timeline.Add);
        }

        private void ReflectCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    this._timeline.Insert(e.NewStartingIndex, GenerateStatusViewModel((TwitterStatus)e.NewItems[0]));
                    break;
                case NotifyCollectionChangedAction.Move:
                    this._timeline.Move(e.OldStartingIndex, e.NewStartingIndex);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    this.Timeline.RemoveAt(e.OldStartingIndex);
                    break;
                case NotifyCollectionChangedAction.Reset:
                    var collection = Model.Timeline.Statuses.ToArray();
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

        public string Name
        {
            get { return _model.Name; }
            set
            {
                _model.Name = value;
                RaisePropertyChanged(() => Name);
            }
        }

        private bool _isSelected = false;
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
                RaisePropertyChanged(() => IsSelected);
            }
        }

        public void Select()
        {
            _owner.SetSelected(this.Model);
        }

        private readonly ObservableCollection<StatusViewModel> _timeline;
        public ObservableCollection<StatusViewModel> Timeline
        {
            get { return _timeline; }
        }

        private int _unreadCount = 0;
        public int UnreadCount
        {
            get { return _unreadCount; }
            set
            {
                if (IsSelected)
                    _unreadCount = 0;
                else
                    _unreadCount = value;
                RaisePropertyChanged(() => UnreadCount);
                RaisePropertyChanged(() => IsUnreadExisted);
            }
        }

        public bool IsUnreadExisted
        {
            get { return UnreadCount > 0; }
        }

        private bool _isLoading = false;
        public bool IsLoading
        {
            get { return _isLoading; }
            set
            {
                _isLoading = value;
                RaisePropertyChanged(() => IsLoading);
            }
        }

        private bool _isMouseOver = false;
        public bool IsMouseOver
        {
            get { return _isMouseOver; }
            set
            {
                _isMouseOver = value;
                RaisePropertyChanged(() => IsMouseOver);
                RaisePropertyChanged(() => IsScrollLock);
            }
        }

        private double _scrollIndex = 0;
        public double ScrollIndex
        {
            get { return _scrollIndex; }
            set
            {
                _scrollIndex = value;
                RaisePropertyChanged(() => ScrollIndex);
                RaisePropertyChanged(() => IsScrollLock);
            }
        }

        private bool _isScrollLockExplicit = false;
        public bool IsScrollLockExplicit
        {
            get { return _isScrollLockExplicit; }
            set { _isScrollLockExplicit = value; }
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
                        return _scrollIndex != 0;
                    case ScrollLockStrategy.Explicit:
                        return IsScrollLockExplicit;
                    default:
                        return false;
                }
            }
        }

        public void ReadMore()
        {
            ReadMore(_model.Timeline.Statuses.Select(_ => _.Id).Min());
        }

        public void ReadMore(long id)
        {
            this.IsSuppressTimelineAutoTrim = true;
            this.IsLoading = true;
            Model.Timeline.ReadMore(id)
                .Finally(() => this.IsLoading = false)
                .OnErrorResumeNext(Observable.Empty<Unit>())
                .Subscribe();
        }

        public void ReadMoreFromWeb(long? id)
        {
            this.IsSuppressTimelineAutoTrim = true;
            this.IsLoading = true;
            Model.ReceiveTimelines(id)
                .Finally(() => this.IsLoading = false)
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
            get { return Timeline.Where(_ => _.IsSelected).FirstOrDefault() != null; }
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
