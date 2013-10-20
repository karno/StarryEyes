using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;
using Livet;
using Livet.EventListeners;
using StarryEyes.Models;
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
        private readonly object _timelineLock = new object();
        private readonly ObservableCollection<StatusViewModel> _timeline;
        private readonly TimelineModelBase _model;

        private bool _isLoading;
        private bool _isMouseOver;
        private bool _isScrollInBottom;
        private bool _isScrollInTop;
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
                System.Diagnostics.Debug.WriteLine("load state: " + value);
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
                this._model.IsAutoTrimEnabled = true;
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

        public TimelineViewModelBase(TimelineModelBase model)
        {
            this._model = model;
            this._timeline = new ObservableCollection<StatusViewModel>();
            DispatcherHolder.Enqueue(this.InitializeCollection, DispatcherPriority.Background);
            this.CompositeDisposable.Add(
                new EventListener<Action<bool>>(
                    h => model.InvalidationStateChanged += h,
                    h => model.InvalidationStateChanged -= h,
                    s => this.IsLoading = s));
            this.CompositeDisposable.Add(() =>
            {
                if (_listener != null) _listener.Dispose();
            });
            this.CompositeDisposable.Add(
                this.ListenPropertyChanged(() => this.CurrentAccounts)
                    .ObserveOnDispatcher()
                    .Select(_ => this.CurrentAccounts.ToArray())
                    .Subscribe(a => _timeline.ForEach(s => s.BindingAccounts = a)));
            this.IsLoading = true;
            this._model.InvalidateTimeline();
        }

        public void ReadMore()
        {
            if (IsScrollInTop || IsLoading) return;
            ReadMore(this._model.Statuses
                           .Select(s => s.Status.Id)
                           .Append(long.MaxValue)
                           .Min());
        }

        protected virtual void ReadMore(long id)
        {
            if (IsScrollInTop || IsLoading) return;
            this._model.IsAutoTrimEnabled = false;
            Task.Run(async () =>
            {
                this.IsLoading = true;
                await this._model.ReadMore(id == long.MaxValue ? (long?)null : id);
                this.IsLoading = false;
            });
        }

        #region Selection Control

        public bool IsSelectedStatusExisted
        {
            get { return this.Timeline.FirstOrDefault(_ => _.IsSelected) != null; }
        }

        public void OnSelectionUpdated()
        {
            RaisePropertyChanged(() => IsSelectedStatusExisted);
            // TODO: Impl
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
            InputAreaModel.SetText(CurrentAccounts.ToArray(), users + " ");
            DeselectAll();
        }

        public void FavoriteSelecteds()
        {
            var accounts = CurrentAccounts
                .Select(Setting.Accounts.Get)
                .Where(a => a != null)
                .ToArray();
            if (accounts.Length == 0)
            {
                var msg = new TaskDialogMessage(new TaskDialogOptions
                            {
                                CommonButtons = TaskDialogCommonButtons.Close,
                                MainIcon = VistaTaskDialogIcon.Error,
                                MainInstruction = "ツイートをお気に入り登録できません。",
                                Content = "アカウントが選択されていません。",
                                Title = "クイックアクション エラー"
                            });
                this.Messenger.Raise(msg);
                return;
            }
            SelectedStatuses
                .Where(s => s.CanFavorite && !s.IsFavorited)
                .ForEach(s => s.Favorite(accounts, true));
            DeselectAll();
        }

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
            if (FocusedStatus == null) return;
            var index = this.Timeline.IndexOf(FocusedStatus) - 1;
            FocusedStatus = index < 0 ? null : this.Timeline[index];
        }

        public void FocusDown()
        {
            if (FocusedStatus == null)
            {
                FocusTop();
                return;
            }
            var index = this.Timeline.IndexOf(FocusedStatus) + 1;
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

        public abstract void GotFocus();

        #endregion

        private IDisposable _listener = null;
        private void InitializeCollection()
        {
            // on dispatcher.
            if (_listener != null)
            {
                _listener.Dispose();
                _listener = null;
            }
            lock (this._timelineLock)
            {
                var sts = this._model.Statuses.SynchronizedToArray(
                    () => _listener = this._model.Statuses
                                          .ListenCollectionChanged()
                                          .Subscribe(e => DispatcherHolder.Enqueue(
                                              () => this.ReflectCollectionChanged(e),
                                              DispatcherPriority.Background)));
                this._timeline.Clear();
                sts.OrderByDescending(s => s.Status.CreatedAt)
                   .Select(this.GenerateStatusViewModel)
                   .ForEach(this._timeline.Add);
            }
        }

        private void ReflectCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            try
            {
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
                            removal.Dispose();
                            break;
                        case NotifyCollectionChangedAction.Reset:
                            var cache = this._timeline.ToArray();
                            this._timeline.Clear();
                            Task.Run(() => cache.ForEach(c => c.Dispose()));
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
                var array = await DispatcherHolder.Dispatcher.BeginInvoke(() =>
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
