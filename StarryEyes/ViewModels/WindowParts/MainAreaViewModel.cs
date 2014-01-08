using System;
using System.Linq;
using System.Reactive.Linq;
using Livet;
using StarryEyes.Models;
using StarryEyes.Models.Timelines.Tabs;
using StarryEyes.Settings;
using StarryEyes.ViewModels.Timelines;
using StarryEyes.ViewModels.Timelines.Statuses;
using StarryEyes.ViewModels.Timelines.Tabs;

namespace StarryEyes.ViewModels.WindowParts
{
    public class MainAreaViewModel : ViewModel
    {
        private readonly ReadOnlyDispatcherCollectionRx<ColumnViewModel> _columns;

        public MainAreaViewModel()
        {
            CompositeDisposable.Add(
                _columns =
                ViewModelHelperRx.CreateReadOnlyDispatcherCollectionRx(
                    TabManager.Columns,
                    cm => new ColumnViewModel(this, cm),
                    DispatcherHelper.UIDispatcher));
            CompositeDisposable.Add(
                Observable.FromEvent(
                    h => TabManager.CurrentFocusColumnChanged += h,
                    h => TabManager.CurrentFocusColumnChanged -= h)
                          .Select(_ => TabManager.CurrentFocusColumnIndex)
                          .Subscribe(UpdateFocusFromModel));
            RegisterEvents();
        }

        public void StartDragDrop()
        {
            Columns.ForEach(c => c.IsDragDropping = true);
        }

        public void FinishDragDrop()
        {
            Columns.ForEach(c => c.IsDragDropping = false);
        }

        public ReadOnlyDispatcherCollectionRx<ColumnViewModel> Columns
        {
            get { return _columns; }
        }

        public ColumnViewModel FocusedColumn
        {
            get { return _columns[TabManager.CurrentFocusColumnIndex]; }
            set
            {
                var index = _columns.IndexOf(value);
                if (TabManager.CurrentFocusColumnIndex == index) return;
                TabManager.CurrentFocusColumnIndex = index;
                _columns.ForEach(c => c.UpdateFocus());
                RaisePropertyChanged();
            }
        }

        private void UpdateFocusFromModel(int newFocus)
        {
            DispatcherHolder.Enqueue(() =>
            {
                _columns.ForEach(c => c.UpdateFocus());
                RaisePropertyChanged(() => FocusedColumn);
            });
        }

        public void CloseTab(ColumnViewModel column, TabViewModel tab)
        {
            var ci = _columns.IndexOf(column);
            var ti = column.Tabs.IndexOf(tab);
            TabManager.CloseTab(ci, ti);
        }

        void MainWindowModelTimelineFocusRequested(TimelineFocusRequest req)
        {
            switch (req)
            {
                case TimelineFocusRequest.TopOfTimeline:
                    ExecuteTimelineAction(tlvm => tlvm.FocusTop());
                    break;
                case TimelineFocusRequest.BottomOfTimeline:
                    ExecuteTimelineAction(tlvm => tlvm.FocusBottom());
                    break;
                case TimelineFocusRequest.AboveStatus:
                    ExecuteTimelineAction(tlvm => tlvm.FocusUp());
                    break;
                case TimelineFocusRequest.BelowStatus:
                    ExecuteTimelineAction(tlvm => tlvm.FocusDown());
                    break;
            }
        }

        #region Key assign control

        private static bool _isRegisteredEvents;

        private void RegisterEvents()
        {
            if (_isRegisteredEvents) throw new InvalidOperationException();
            _isRegisteredEvents = true;
            MainWindowModel.TimelineFocusRequested += this.MainWindowModelTimelineFocusRequested;
            // Timeline actions
            KeyAssignManager.RegisterActions(
                KeyAssignAction.Create("CreateTab", () => FocusedColumn.CreateNewTab()),
                KeyAssignAction.Create("ToggleFocus", () => ExecuteStatusAction(s => s.ToggleFocus())),
                KeyAssignAction.Create("ToggleSelect", () => ExecuteStatusAction(s => s.ToggleSelect())),
                KeyAssignAction.Create("ClearSelect", () => ExecuteTimelineAction(t => t.DeselectAll())),
                KeyAssignAction.Create("Favorite", () => ExecuteStatusAction(s => s.ToggleFavoriteImmediate())),
                KeyAssignAction.Create("FavoriteMany", () => ExecuteStatusAction(s => s.ToggleFavorite())),
                KeyAssignAction.Create("Retweet", () => ExecuteStatusAction(s => s.ToggleRetweetImmediate())),
                KeyAssignAction.Create("RetweetMany", () => ExecuteStatusAction(s => s.ToggleRetweet())),
                KeyAssignAction.Create("Quote", () => ExecuteStatusAction(s => s.Quote())),
                KeyAssignAction.Create("QuoteLink", () => ExecuteStatusAction(s => s.QuotePermalink())),
                KeyAssignAction.Create("SendDirectMessage", () => ExecuteStatusAction(s => s.DirectMessage())),
                KeyAssignAction.Create("Delete", () => ExecuteStatusAction(s => s.ConfirmDelete())),
                KeyAssignAction.Create("Copy", () => ExecuteStatusAction(s => s.CopyBody())),
                KeyAssignAction.Create("CopySTOT", () => ExecuteStatusAction(s => s.CopySTOT())),
                KeyAssignAction.Create("CopyPermalink", () => ExecuteStatusAction(s => s.CopyPermalink())),
                KeyAssignAction.Create("ShowUserProfile", () => ExecuteStatusAction(s => s.ShowUserProfile())),
                KeyAssignAction.Create("ShowRetweeterProfile", () => ExecuteStatusAction(s => s.ShowRetweeterProfile())),
                KeyAssignAction.Create("OpenWeb", () => ExecuteStatusAction(s => s.OpenWeb())),
                KeyAssignAction.Create("OpenFavstar", () => ExecuteStatusAction(s => s.OpenFavstar())),
                KeyAssignAction.Create("OpenUserDetailOnTwitter",
                                       () => ExecuteStatusAction(s => s.OpenUserDetailOnTwitter())),
                KeyAssignAction.Create("OpenUserFavstar", () => ExecuteStatusAction(s => s.OpenUserFavstar())),
                KeyAssignAction.Create("OpenUserTwilog", () => ExecuteStatusAction(s => s.OpenUserTwilog())),
                KeyAssignAction.Create("OpenSource", () => ExecuteStatusAction(s => s.OpenSourceLink())),
                KeyAssignAction.Create("OpenThumbnail", () => ExecuteStatusAction(s => s.OpenThumbnailImage())),
                KeyAssignAction.Create("OpenConversation", () => ExecuteStatusAction(s => s.ShowConversation())),
                KeyAssignAction.Create("MuteUser", () => ExecuteStatusAction(s => s.MuteUser())),
                KeyAssignAction.Create("MuteClient", () => ExecuteStatusAction(s => s.MuteClient())),
                KeyAssignAction.Create("ReportAsSpam", () => ExecuteStatusAction(s => s.ReportAsSpam())),
                KeyAssignAction.Create("GiveTrophy", () => ExecuteStatusAction(s => s.GiveFavstarTrophy())),
                KeyAssignAction.CreateWithArgumentOptional("Reply", a => this.ExecuteStatusAction(s => s.SendReplyOrDirectMessage(a))),
                KeyAssignAction.CreateWithArgumentRequired("OpenUrl",
                                                           a => this.ExecuteStatusAction(s => s.OpenNthLink(a)))
                );

            // Timeline argumentable actions
            // reply, favorite, retweet, quote
            // TODO

        }

        internal void ExecuteTimelineAction(Action<TimelineViewModelBase> action)
        {
            var timeline = TimelineActionTargetOverride ?? FocusedColumn.FocusedTab;
            if (timeline == null) return;
            DispatcherHolder.Enqueue(() => action(timeline));
        }

        internal void ExecuteStatusAction(Action<StatusViewModel> status)
        {
            var timeline = TimelineActionTargetOverride ?? FocusedColumn.FocusedTab;
            StatusViewModel target;
            if (timeline == null || (target = timeline.FocusedStatus) == null) return;
            status(target);
        }

        private static TimelineViewModelBase _timelineActionTargetOverride;
        internal static TimelineViewModelBase TimelineActionTargetOverride
        {
            get { return _timelineActionTargetOverride; }
            set
            {
                _timelineActionTargetOverride = value;
                System.Diagnostics.Debug.WriteLine("* set key assign override: " +
                                                   (value == null ? "null" : value.GetType().ToString()));
            }
        }

        #endregion
    }
}
