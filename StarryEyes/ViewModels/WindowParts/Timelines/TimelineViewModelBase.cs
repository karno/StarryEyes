using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using System.Windows.Threading;
using Livet;
using Livet.EventListeners;
using StarryEyes.Models;
using StarryEyes.Models.Tab;
using StarryEyes.Nightmare.Windows;
using StarryEyes.Settings;
using StarryEyes.Views.Messaging;

namespace StarryEyes.ViewModels.WindowParts.Timelines
{
    public abstract class TimelineViewModelBase : ViewModel
    {
        private readonly object _collectionLock = new object();
        private volatile bool _isCollectionAddEnabled;

        private bool _isLoading;
        private bool _isMouseOver;
        private bool _isScrollInBottom;
        private bool _isScrollInTop;
        private bool _isScrollLockExplicit;
        private StatusViewModel _focusedStatus;
        private ObservableCollection<StatusViewModel> _timeline;
        private IDisposable _currentTimelineListener;

        protected TimelineViewModelBase()
        {
            this.CompositeDisposable.Add(async () =>
            {
                if (_currentTimelineListener != null)
                {
                    _currentTimelineListener.Dispose();
                }
                if (this._timeline == null) return;
                var array = await DispatcherHolder.Dispatcher.BeginInvoke(() =>
                {
                    lock (this._collectionLock)
                    {
                        var pta = this._timeline.ToArray();
                        this._timeline.Clear();
                        return pta;
                    }
                }, DispatcherPriority.ContextIdle);
                array.ForEach(a => a.Dispose());
            });
        }

        public ObservableCollection<StatusViewModel> Timeline
        {
            get
            {
                if (_timeline == null)
                {
                    var ctl = _timeline = new ObservableCollection<StatusViewModel>();
                    DispatcherHolder.Enqueue(() => InitializeCollection(ctl), DispatcherPriority.Background);
                }
                return _timeline;
            }
        }

        private void InitializeCollection(ObservableCollection<StatusViewModel> ctl)
        {
            lock (_collectionLock)
            {
                if (_currentTimelineListener != null)
                {
                    throw new InvalidOperationException();
                }
                var composite = new CompositeDisposable();
                _currentTimelineListener = composite;
                composite.Add(Disposable.Create(() => _isCollectionAddEnabled = false));
                composite.Add(
                    new CollectionChangedEventListener(
                        TimelineModel.Statuses,
                        (sender, e) =>
                        {
                            if (!_isCollectionAddEnabled) return;
                            DispatcherHolder.Enqueue(
                                () => ReflectCollectionChanged(e, ctl),
                                DispatcherPriority.Background);
                        }));
                var collection = TimelineModel.Statuses.SynchronizedToArray(() => _isCollectionAddEnabled = true);
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
                        ctl.Insert(e.NewStartingIndex, GenerateStatusViewModel((StatusModel)e.NewItems[0]));
                        break;
                    case NotifyCollectionChangedAction.Move:
                        ctl.Move(e.OldStartingIndex, e.NewStartingIndex);
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        var removal = ctl[e.OldStartingIndex];
                        ctl.RemoveAt(e.OldStartingIndex);
                        removal.Dispose();
                        break;
                    case NotifyCollectionChangedAction.Reset:
                        var cache = ctl.ToArray();
                        ctl.Clear();
                        Task.Run(() => cache.ForEach(c => c.Dispose()));
                        break;
                    default:
                        throw new ArgumentException();
                }
            }
        }

        protected StatusViewModel GenerateStatusViewModel(StatusModel status)
        {
            return new StatusViewModel(this, status, CurrentAccounts);
        }

        /// <summary>
        /// Clear timeline cache
        /// </summary>
        protected void ReInitializeTimeline()
        {
            var prevTimeline = _timeline;
            var prevListener = _currentTimelineListener;
            if (prevListener == null && prevTimeline == null) return;
            _timeline = null;
            _currentTimelineListener = null;
            RaisePropertyChanged(() => Timeline);
            Task.Run(async () =>
            {
                if (prevListener != null)
                {
                    prevListener.Dispose();
                }
                if (prevTimeline == null) return;
                var array = await DispatcherHolder.Dispatcher.BeginInvoke(() =>
                {
                    lock (_collectionLock)
                    {
                        var pta = prevTimeline.ToArray();
                        prevTimeline.Clear();
                        return pta;
                    }
                }, DispatcherPriority.ContextIdle);
                array.ForEach(a => a.Dispose());
            });
        }

        protected abstract TimelineModel TimelineModel { get; }

        protected abstract IEnumerable<long> CurrentAccounts { get; }

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
                TimelineModel.IsSuppressTimelineTrimming = false;
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

        public void ReadMore()
        {
            if (IsScrollInTop || !_isCollectionAddEnabled || IsLoading) return;
            ReadMore(TimelineModel.Statuses
                                  .Select(s => s.Status.Id)
                                  .Append(long.MaxValue)
                                  .Min());
        }

        protected virtual void ReadMore(long id)
        {
            if (IsScrollInTop || !_isCollectionAddEnabled || IsLoading) return;
            TimelineModel.IsSuppressTimelineTrimming = true;
            IsLoading = true;
            Task.Run(async () =>
            {
                try
                {
                    await this.TimelineModel.ReadMore(id == long.MaxValue ? (long?)null : id);
                }
                catch
                {
                }
                finally
                {
                    IsLoading = false;
                }
            });
        }

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

        private IEnumerable<StatusViewModel> SelectedStatuses
        {
            get
            {
                return Timeline.Where(s => s.IsSelected);
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
            TabManager.CreateTab(
                new TabModel("extracted",
                             "from all where " +
                             users.Select(u => "user == \"^" + u + "$\"")
                                  .JoinString("||")
                    ));
            DeselectAll();
        }

        public void DeselectAll()
        {
            Timeline.ForEach(s => s.IsSelected = false);
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
            var index = Timeline.IndexOf(FocusedStatus) - 1;
            FocusedStatus = index < 0 ? null : Timeline[index];
        }

        public void FocusDown()
        {
            if (FocusedStatus == null)
            {
                FocusTop();
                return;
            }
            var index = Timeline.IndexOf(FocusedStatus) + 1;
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

        public abstract void GotPhysicalFocus();

        #endregion
    }
}
