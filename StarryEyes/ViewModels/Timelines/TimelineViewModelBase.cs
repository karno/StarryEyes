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

        public ObservableCollection<StatusViewModel> Timeline
        {
            get { return this._timeline; }
        }

        protected abstract IEnumerable<long> CurrentAccounts { get; }

        public bool IsLoading
        {
            get { return _isLoading; }
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
            get { return _isMouseOver; }
            set
            {
                if (_isMouseOver == value) return;
                _isMouseOver = value;
                RaisePropertyChanged();
                RaisePropertyChanged(() => this.IsScrollLockEnabled);
            }
        }

        public bool IsScrollOnTop
        {
            get { return this._isScrollOnTop; }
            set
            {
                if (this._isScrollOnTop == value) return;
                this._isScrollOnTop = value;
                RaisePropertyChanged();
                // RaisePropertyChanged(() => IsScrollLockEnabled);
                // revive auto trim
                if (!this._model.IsAutoTrimEnabled)
                {
                    this._model.IsAutoTrimEnabled = true;
                    _readCount = 0;
                }
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
                if (value)
                {
                    this.ReadMore();
                }
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
                RaisePropertyChanged(() => this.IsScrollLockEnabled);
            }
        }

        public ScrollUnit ScrollUnit
        {
            get { return Setting.IsScrollByPixel.Value ? ScrollUnit.Pixel : ScrollUnit.Item; }
        }

        public bool IsScrollLockEnabled
        {
            get
            {
                if (this.IsLoading)
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

        public bool IsScrollLockOnlyScrolled
        {
            get { return Setting.ScrollLockStrategy.Value == ScrollLockStrategy.WhenScrolled; }
        }

        public bool IsAnimationEnabled
        {
            get
            {
                if (this.IsLoading)
                {
                    // when (re)loading, skip scrolling action.
                    return false;
                }
                return Setting.IsScrollByPixel.Value && Setting.IsAnimateScrollToNewTweet.Value;
            }
        }

        public TimelineViewModelBase(TimelineModelBase model)
        {
            this._model = model;
            this._timeline = new ObservableCollection<StatusViewModel>();
            DispatcherHelper.UIDispatcher.InvokeAsync(this.InitializeCollection, DispatcherPriority.Background);
            this.CompositeDisposable.Add(
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
                            DispatcherHelper.UIDispatcher.InvokeAsync(() =>
                            {
                                this.IsLoading = true;
                            }, DispatcherPriority.Send);
                        }
                        else
                        {
                            // wait for dispatcher
                            DispatcherHelper.UIDispatcher.InvokeAsync(() =>
                            {
                                // this clause is invoked later, so re-check currrent value
                                if (model.IsLoading == false)
                                {
                                    System.Diagnostics.Debug.WriteLine("INVALIDATION COMPLETED!");
                                    this.IsLoading = false;
                                }
                            }, DispatcherPriority.ContextIdle);
                        }
                    }));
            this.CompositeDisposable.Add(() =>
            {
                if (_listener != null) _listener.Dispose();
            });
            this.CompositeDisposable.Add(
                this.ListenPropertyChanged(() => this.CurrentAccounts,
                    e => DispatcherHelper.UIDispatcher.InvokeAsync(() =>
                    {
                        var a = this.CurrentAccounts.ToArray();
                        this._timeline.ForEach(s => s.BindingAccounts = a);
                    })));
            this.CompositeDisposable.Add(
                Setting.ScrollLockStrategy.ListenValueChanged(
                    v =>
                    {
                        RaisePropertyChanged(() => this.IsScrollLockEnabled);
                        RaisePropertyChanged(() => this.IsScrollLockOnlyScrolled);
                    }));
            this.CompositeDisposable.Add(
                Setting.IsAnimateScrollToNewTweet.ListenValueChanged(
                    v => RaisePropertyChanged(() => IsAnimationEnabled)));
            this.CompositeDisposable.Add(
                Setting.IsScrollByPixel.ListenValueChanged(
                    v =>
                    {
                        RaisePropertyChanged(() => ScrollUnit);
                        RaisePropertyChanged(() => IsAnimationEnabled);
                    }));
            this._model.InvalidateTimeline();
        }

        public void ReadMore()
        {
            if (this.IsScrollOnTop || IsLoading) return;
            // get minimum status id in this timeline
            var minId = this._model.Statuses
                            .Select(s => s.Status.Id)
                            .Append(long.MaxValue)
                            .Min();
            ReadMore(minId);
        }

        public void ReadMore(long id)
        {
            if (IsLoading) return;
            if (!this._model.IsAutoTrimEnabled)
            {
                // check read-back count
                if (_readCount >= MaxReadCount) return;
                _readCount++;
            }
            else
            {
                // disable auto trimming
                this._model.IsAutoTrimEnabled = false;
            }
            Task.Run(() => _model.ReadMore(id == long.MaxValue ? (long?)null : id));
        }

        #region Selection Control

        public bool IsStatusSelected
        {
            get { return this.Timeline.FirstOrDefault(_ => _.IsSelected) != null; }
        }

        public void OnSelectionUpdated()
        {
            RaisePropertyChanged(() => this.IsStatusSelected);
        }

        private IEnumerable<StatusViewModel> SelectedStatuses
        {
            get
            {
                return this.Timeline.Where(s => s.IsSelected);
            }
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
                this.Messenger.RaiseSafe(() =>
                    new TaskDialogMessage(new TaskDialogOptions
                    {
                        Title = MainAreaTimelineResources.MsgQuickActionFailedTitle,
                        MainIcon = VistaTaskDialogIcon.Error,
                        MainInstruction = MainAreaTimelineResources.MsgFavoriteFailedInst,
                        Content = MainAreaTimelineResources.MsgQuickActionAccountIsNotSelected,
                        CommonButtons = TaskDialogCommonButtons.Close,
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
            this.Timeline.ForEach(s => s.IsSelected = false);
        }

        #endregion

        #region Focus Control

        public void Focus()
        {
            this._model.RequestFocus();
        }

        public StatusViewModel FocusedStatus
        {
            get { return _focusedStatus; }
            set
            {
                var previous = _focusedStatus;
                _focusedStatus = value;
                if (previous != null)
                {
                    previous.RaiseFocusedChanged();
                }
                if (value != null)
                {
                    value.RaiseFocusedChanged();
                    var index = this.Timeline.IndexOf(value);
                    this.Messenger.RaiseSafe(() => new ScrollIntoViewMessage(index));
                }
            }
        }

        public void FocusUp()
        {
            if (Timeline.Count == 0 || FocusedStatus == null) return;
            var index = this.Timeline.IndexOf(FocusedStatus) - 1;
            FocusedStatus = index < 0 ? null : this.Timeline[index];
        }

        public void FocusDown()
        {
            if (Timeline.Count == 0) return;
            var index = FocusedStatus == null
                            ? 0
                            : this.Timeline.IndexOf(FocusedStatus) + 1;
            if (index >= this.Timeline.Count) return;
            FocusedStatus = this.Timeline[index];
        }

        public void FocusTop()
        {
            if (this.Timeline.Count == 0) return;
            FocusedStatus = this.Timeline[0];
        }

        public void FocusBottom()
        {
            if (this.Timeline.Count == 0) return;
            FocusedStatus = this.Timeline[this.Timeline.Count - 1];
        }

        // called from xaml
        [UsedImplicitly]
        public abstract void GotFocus();

        #endregion

        private IDisposable _listener;
        private void InitializeCollection()
        {
            if (this._disposed) return;
            // on dispatcher.
            if (_listener != null)
            {
                _listener.Dispose();
                _listener = null;
            }
            lock (this._timelineLock)
            {
                var sts = this._model.Statuses.SynchronizedToArray(
                    () => _listener = this._model.Statuses.ListenCollectionChanged(
                        e => DispatcherHelper.UIDispatcher.InvokeAsync(
                            () => this.ReflectCollectionChanged(e),
                            DispatcherPriority.Background)));
                var items = _timeline.ToArray();
                this._timeline.Clear();
                sts.OrderByDescending(s => s.Status.CreatedAt)
                   .Select(this.GenerateStatusViewModel)
                   .ForEach(this._timeline.Add);
                _disposeWorker.Queue(() => items.ForEach(i => i.Dispose()));
            }
        }

        private void ReflectCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            try
            {
                if (this._disposed) return;
                lock (this._timelineLock)
                {
                    switch (e.Action)
                    {
                        case NotifyCollectionChangedAction.Add:
                            this._timeline.Insert(e.NewStartingIndex,
                                GenerateStatusViewModel((StatusModel)e.NewItems[0]));
                            break;
                        case NotifyCollectionChangedAction.Move:
                            this._timeline.Move(e.OldStartingIndex, e.NewStartingIndex);
                            break;
                        case NotifyCollectionChangedAction.Remove:
                            var removal = this._timeline[e.OldStartingIndex];
                            this._timeline.RemoveAt(e.OldStartingIndex);
                            if (removal.IsSelected)
                            {
                                OnSelectionUpdated();
                            }
                            _disposeWorker.Queue(() => removal.Dispose());
                            break;
                        case NotifyCollectionChangedAction.Reset:
                            var cache = this._timeline.ToArray();
                            this._timeline.Clear();
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
                    this.InitializeCollection();
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
                    lock (this._timelineLock)
                    {
                        var pta = this._timeline.ToArray();
                        this._timeline.Clear();
                        return pta;
                    }
                });
                array.ForEach(a => a.Dispose());
            });
        }
    }
}
