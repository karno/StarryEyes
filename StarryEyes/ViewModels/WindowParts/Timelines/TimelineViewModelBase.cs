using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Windows.Threading;
using Livet;
using Livet.EventListeners;
using StarryEyes.Breezy.DataModel;
using StarryEyes.Models;
using StarryEyes.Models.Stores;
using StarryEyes.Models.Tab;
using StarryEyes.Settings;
using StarryEyes.Views.Messaging;
using TaskDialogInterop;

namespace StarryEyes.ViewModels.WindowParts.Timelines
{
    public abstract class TimelineViewModelBase : ViewModel
    {
        private readonly object _collectionLock = new object();

        private bool _isLoading;
        private bool _isMouseOver;
        private bool _isScrollInBottom;
        private bool _isScrollInTop;
        private bool _isScrollLockExplicit;
        private StatusViewModel _focusedStatus;
        private ObservableCollection<StatusViewModel> _timeline;

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

        private volatile bool _isCollectionAddEnabled;
        private void InitializeCollection(ObservableCollection<StatusViewModel> ctl)
        {
            lock (_collectionLock)
            {
                CompositeDisposable.Add(
                    new CollectionChangedEventListener(
                        TimelineModel.Statuses,
                        (sender, e) =>
                        {
                            if (_isCollectionAddEnabled)
                            {
                                DispatcherHolder.Enqueue(
                                    () => ReflectCollectionChanged(e, ctl),
                                    DispatcherPriority.Background);
                            }
                        }));
                TwitterStatus[] collection = TimelineModel.Statuses.ToArray();
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

        private void Rebind(ObservableCollection<StatusViewModel> ctl)
        {
            lock (_collectionLock)
            {
                _isCollectionAddEnabled = false;
                StatusViewModel[] cache = ctl.ToArray();
                ctl.Clear();
                cache.ToObservable()
                     .ObserveOn(TaskPoolScheduler.Default)
                     .Subscribe(_ => _.Dispose());
                TwitterStatus[] collection = TimelineModel.Statuses.ToArray();
                _isCollectionAddEnabled = true;
                collection
                    .Select(GenerateStatusViewModel)
                    .ForEach(ctl.Add);
            }
        }

        protected StatusViewModel GenerateStatusViewModel(TwitterStatus status)
        {
            return new StatusViewModel(this, status, CurrentAccounts);
        }

        protected void ReInitializeTimeline()
        {
            _timeline = null;
            RaisePropertyChanged(() => Timeline);
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

        public void ReadMore()
        {
            ReadMore(TimelineModel.Statuses.Select(_ => _.Id).Min());
        }

        public virtual void ReadMore(long id)
        {
            IsSuppressTimelineAutoTrim = true;
            IsLoading = true;
            TimelineModel.ReadMore(id)
                 .Finally(() => IsLoading = false)
                 .OnErrorResumeNext(Observable.Empty<Unit>())
                 .Subscribe();
        }

        #region Call by code-behind

        public bool IsSuppressTimelineAutoTrim
        {
            get { return TimelineModel.IsSuppressTimelineTrimming; }
            set { TimelineModel.IsSuppressTimelineTrimming = value; }
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
                .Select(AccountsStore.GetAccountSetting)
                .Where(a => a != null)
                .Select(a => a.AuthenticateInfo)
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
            MainAreaModel.CreateTab(
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

        public abstract void GotPhysicalFocus();

        #endregion
    }
}
