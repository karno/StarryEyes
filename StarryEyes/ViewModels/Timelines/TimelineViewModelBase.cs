using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Threading;
using JetBrains.Annotations;
using Livet;
using Livet.EventListeners;
using StarryEyes.Albireo.Helpers;
using StarryEyes.Albireo.Threading;
using StarryEyes.Globalization.WindowParts;
using StarryEyes.Models.Inputting;
using StarryEyes.Models.Timelines;
using StarryEyes.Models.Timelines.Statuses;
using StarryEyes.Models.Timelines.Tabs;
using StarryEyes.Nightmare.Windows;
using StarryEyes.Settings;
using StarryEyes.ViewModels.Timelines.Statuses;
using StarryEyes.Views.Messaging;

namespace StarryEyes.ViewModels.Timelines
{
    public abstract class TimelineViewModelBase : ViewModel
    {
        private static readonly SerialTaskWorker _disposeWorker;

        static TimelineViewModelBase()
        {
            _disposeWorker = new SerialTaskWorker();
            App.ApplicationFinalize += () => _disposeWorker.Dispose();
        }

        /// <summary>
        /// maximum read-back count
        /// </summary>
        private const int MaxReadCount = 5;

        private readonly object _timelineLock = new object();
        private readonly ObservableCollection<StatusViewModel> _timeline;
        private readonly TimelineModelBase _model;

        private int _readCount;
        private bool _isLoading;
        private bool _isMouseOver;
        private bool _isScrollInBottom;
        private bool _isScrollOnTop;
        private bool _isScrollLockExplicit;
        private StatusViewModel _focusedStatus;

        public ObservableCollection<StatusViewModel> Timeline => _timeline;

        protected abstract IEnumerable<long> CurrentAccounts { get; }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading == value) return;
                _isLoading = value;
                RaisePropertyChanged();
                RaisePropertyChanged(() => IsScrollLockEnabled);
                RaisePropertyChanged(() => IsAnimationEnabled);
            }
        }

        public bool IsMouseOver
        {
            get => _isMouseOver;
            set
            {
                if (_isMouseOver == value) return;
                _isMouseOver = value;
                RaisePropertyChanged();
                RaisePropertyChanged(() => IsScrollLockEnabled);
            }
        }

        public bool IsScrollOnTop
        {
            get => _isScrollOnTop;
            set
            {
                if (_isScrollOnTop == value) return;
                _isScrollOnTop = value;
                RaisePropertyChanged();
                // RaisePropertyChanged(() => IsScrollLockEnabled);
                // revive auto trim
                if (!_model.IsAutoTrimEnabled)
                {
                    _model.IsAutoTrimEnabled = true;
                    _readCount = 0;
                }
            }
        }

        public bool IsScrollInBottom
        {
            get => _isScrollInBottom;
            set
            {
                if (_isScrollInBottom == value) return;
                _isScrollInBottom = value;
                RaisePropertyChanged();
                if (value)
                {
                    ReadMore();
                }
            }
        }

        public bool IsScrollLockExplicit
        {
            get => _isScrollLockExplicit;
            set
            {
                if (_isScrollLockExplicit == value) return;
                _isScrollLockExplicit = value;
                RaisePropertyChanged();
                RaisePropertyChanged(() => IsScrollLockEnabled);
            }
        }

        public ScrollUnit ScrollUnit => Setting.IsScrollByPixel.Value ? ScrollUnit.Pixel : ScrollUnit.Item;

        public bool IsScrollLockEnabled
        {
            get
            {
                if (IsLoading)
                {
                    // when (re)loading, skip scroll-locking.
                    return false;
                }
                switch (Setting.ScrollLockStrategy.Value)
                {
                    case ScrollLockStrategy.None:
                        return false;
                    case ScrollLockStrategy.Always:
                    case ScrollLockStrategy.WhenScrolled:
                        // when scrolled -> consider on TimelineScrollLockBehavior.
                        // cf: IsScrollLockOnlyScrolled property
                        return true;
                    case ScrollLockStrategy.WhenMouseOver:
                        return IsMouseOver;
                    case ScrollLockStrategy.Explicit:
                        return IsScrollLockExplicit;
                    default:
                        return false;
                }
            }
        }

        public bool IsScrollLockOnlyScrolled => Setting.ScrollLockStrategy.Value == ScrollLockStrategy.WhenScrolled;

        public bool IsAnimationEnabled
        {
            get
            {
                if (IsLoading)
                {
                    // when (re)loading, skip scrolling action.
                    return false;
                }
                return Setting.IsScrollByPixel.Value && Setting.IsAnimateScrollToNewTweet.Value;
            }
        }

        protected TimelineViewModelBase(TimelineModelBase model)
        {
            _model = model;
            _timeline = new ObservableCollection<StatusViewModel>();
            DispatcherHelper.UIDispatcher.InvokeAsync(InitializeCollection, DispatcherPriority.Background);
            CompositeDisposable.Add(
                new EventListener<Action<bool>>(
                    h => model.IsLoadingChanged += h,
                    h => model.IsLoadingChanged -= h,
                    isLoading =>
                    {
                        if (isLoading)
                        {
                            // send immediate
                            // ! MUST BE DispatcherPriority.Send ! 
                            // for update binding value before beginning rendering.
                            DispatcherHelper.UIDispatcher.InvokeAsync(() => { IsLoading = true; },
                                DispatcherPriority.Send);
                        }
                        else
                        {
                            // wait for dispatcher
                            DispatcherHelper.UIDispatcher.InvokeAsync(() =>
                            {
                                // this clause is invoked later, so re-check currrent value
                                if (model.IsLoading == false)
                                {
                                    IsLoading = false;
                                }
                            }, DispatcherPriority.ContextIdle);
                        }
                    }));
            CompositeDisposable.Add(() => { _listener?.Dispose(); });
            CompositeDisposable.Add(
                this.ListenPropertyChanged(() => CurrentAccounts,
                    e => DispatcherHelper.UIDispatcher.InvokeAsync(() =>
                    {
                        var a = CurrentAccounts.ToArray();
                        _timeline.ForEach(s => s.BindingAccounts = a);
                    })));
            CompositeDisposable.Add(
                Setting.ScrollLockStrategy.ListenValueChanged(
                    v =>
                    {
                        RaisePropertyChanged(() => IsScrollLockEnabled);
                        RaisePropertyChanged(() => IsScrollLockOnlyScrolled);
                    }));
            CompositeDisposable.Add(
                Setting.IsAnimateScrollToNewTweet.ListenValueChanged(
                    v => RaisePropertyChanged(() => IsAnimationEnabled)));
            CompositeDisposable.Add(
                Setting.IsScrollByPixel.ListenValueChanged(
                    v =>
                    {
                        RaisePropertyChanged(() => ScrollUnit);
                        RaisePropertyChanged(() => IsAnimationEnabled);
                    }));
            _model.InvalidateTimeline();
        }

        public void ReadMore()
        {
            if (IsScrollOnTop || IsLoading) return;
            // get minimum status id in this timeline
            var minId = _model.Statuses
                              .Select(s => s.Status.Id)
                              .Append(long.MaxValue)
                              .Min();
            ReadMore(minId);
        }

        public void ReadMore(long id)
        {
            if (IsLoading) return;
            if (!_model.IsAutoTrimEnabled)
            {
                // check read-back count
                if (_readCount >= MaxReadCount) return;
                _readCount++;
            }
            else
            {
                // disable auto trimming
                _model.IsAutoTrimEnabled = false;
            }
            Task.Run(() => _model.ReadMore(id == long.MaxValue ? (long?)null : id));
        }

        #region Selection Control

        public bool IsStatusSelected
        {
            get { return Timeline.FirstOrDefault(_ => _.IsSelected) != null; }
        }

        public void OnSelectionUpdated()
        {
            RaisePropertyChanged(() => IsStatusSelected);
        }

        private IEnumerable<StatusViewModel> SelectedStatuses
        {
            get { return Timeline.Where(s => s.IsSelected); }
        }

        public void ReplySelecteds()
        {
            var users = SelectedStatuses
                .Select(s => "@" + s.User.ScreenName)
                .Distinct()
                .JoinString(" ");
            var accs = CurrentAccounts.Select(id => Setting.Accounts.Get(id))
                                      .Where(a => a != null)
                                      .ToArray();
            InputModel.InputCore.SetText(InputSetting.Create(accs, users + " "));
            DeselectAll();
        }

        [UsedImplicitly]
        public void FavoriteSelecteds()
        {
            var accounts = CurrentAccounts
                .Select(Setting.Accounts.Get)
                .Where(a => a != null)
                .ToArray();
            if (accounts.Length == 0)
            {
                Messenger.RaiseSafe(() =>
                    new TaskDialogMessage(new TaskDialogOptions
                    {
                        Title = MainAreaTimelineResources.MsgQuickActionFailedTitle,
                        MainIcon = VistaTaskDialogIcon.Error,
                        MainInstruction = MainAreaTimelineResources.MsgFavoriteFailedInst,
                        Content = MainAreaTimelineResources.MsgQuickActionAccountIsNotSelected,
                        CommonButtons = TaskDialogCommonButtons.Close
                    }));
                return;
            }
            SelectedStatuses
                .Where(s => s.CanFavorite && !s.IsFavorited)
                .ForEach(s => s.Favorite(accounts, true));
            DeselectAll();
        }

        [UsedImplicitly]
        public void ExtractSelecteds()
        {
            var users = SelectedStatuses
                .Select(s => s.OriginalStatus.User.ScreenName)
                .Distinct();
            TabManager.CreateTab(TabModel.Create(
                "extracted",
                "from all where " +
                users.Select(u => "user == \"" + u + "\"")
                     .JoinString("||")));
            DeselectAll();
        }

        public void DeselectAll()
        {
            Timeline.ForEach(s => s.IsSelected = false);
        }

        #endregion Selection Control

        #region Focus Control

        public void Focus()
        {
            _model.RequestFocus();
        }

        public StatusViewModel FocusedStatus
        {
            get => _focusedStatus;
            set
            {
                var previous = _focusedStatus;
                _focusedStatus = value;
                previous?.RaiseFocusedChanged();
                if (value != null)
                {
                    value.RaiseFocusedChanged();
                    var index = Timeline.IndexOf(value);
                    Messenger.RaiseSafe(() => new ScrollIntoViewMessage(index));
                }
            }
        }

        public void FocusUp()
        {
            if (Timeline.Count == 0 || FocusedStatus == null) return;
            var index = Timeline.IndexOf(FocusedStatus) - 1;
            FocusedStatus = index < 0 ? null : Timeline[index];
        }

        public void FocusDown()
        {
            if (Timeline.Count == 0) return;
            var index = FocusedStatus == null
                ? 0
                : Timeline.IndexOf(FocusedStatus) + 1;
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

        // called from xaml
        [UsedImplicitly]
        public abstract void GotFocus();

        #endregion Focus Control

        private IDisposable _listener;

        private void InitializeCollection()
        {
            if (_disposed) return;
            // on dispatcher.
            if (_listener != null)
            {
                _listener.Dispose();
                _listener = null;
            }
            lock (_timelineLock)
            {
                var sts = _model.Statuses.SynchronizedToArray(
                    () => _listener = _model.Statuses.ListenCollectionChanged(
                        e => DispatcherHelper.UIDispatcher.InvokeAsync(
                            () =>
                                ReflectCollectionChanged(
                                    e),
                            DispatcherPriority
                                .Background)));
                var items = _timeline.ToArray();
                _timeline.Clear();
                sts.OrderByDescending(s => s.Status.CreatedAt)
                   .Select(GenerateStatusViewModel)
                   .ForEach(_timeline.Add);
                _disposeWorker.Queue(() => items.ForEach(i => i.Dispose()));
            }
        }

        private void ReflectCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            try
            {
                if (_disposed) return;
                lock (_timelineLock)
                {
                    switch (e.Action)
                    {
                        case NotifyCollectionChangedAction.Add:
                            _timeline.Insert(e.NewStartingIndex,
                                GenerateStatusViewModel((StatusModel)e.NewItems[0]));
                            break;
                        case NotifyCollectionChangedAction.Move:
                            _timeline.Move(e.OldStartingIndex, e.NewStartingIndex);
                            break;
                        case NotifyCollectionChangedAction.Remove:
                            var removal = _timeline[e.OldStartingIndex];
                            _timeline.RemoveAt(e.OldStartingIndex);
                            if (removal.IsSelected)
                            {
                                OnSelectionUpdated();
                            }
                            _disposeWorker.Queue(() => removal.Dispose());
                            break;
                        case NotifyCollectionChangedAction.Reset:
                            var cache = _timeline.ToArray();
                            _timeline.Clear();
                            _disposeWorker.Queue(() => cache.ForEach(c => c.Dispose()));
                            break;
                        default:
                            throw new ArgumentException();
                    }
                }
            }
            catch (ArgumentOutOfRangeException)
            {
                System.Diagnostics.Debug.WriteLine("*TIMELINE FAIL*");
                // timeline consistency error
                if (!_disposed)
                {
                    InitializeCollection();
                }
            }
        }

        protected StatusViewModel GenerateStatusViewModel(StatusModel status)
        {
            return new StatusViewModel(this, status, CurrentAccounts);
        }

        private bool _disposed;

        protected override void Dispose(bool disposing)
        {
            _disposed = true;
            base.Dispose(disposing);
            if (!disposing) return;
            Task.Run(async () =>
            {
                var array = await DispatcherHelper.UIDispatcher.InvokeAsync(() =>
                {
                    lock (_timelineLock)
                    {
                        var pta = _timeline.ToArray();
                        _timeline.Clear();
                        return pta;
                    }
                });
                array.ForEach(a => a.Dispose());
            });
        }
    }
}