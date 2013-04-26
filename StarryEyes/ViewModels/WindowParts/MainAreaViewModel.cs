using System;
using System.Linq;
using System.Reactive.Linq;
using Livet;
using StarryEyes.Models;
using StarryEyes.Models.Tab;
using StarryEyes.Settings;
using StarryEyes.ViewModels.WindowParts.Timelines;

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
                    h => TabManager.OnCurrentFocusColumnChanged += h,
                    h => TabManager.OnCurrentFocusColumnChanged -= h)
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

        private int _oldFocus;
        public ColumnViewModel FocusedColumn
        {
            get { return _columns[TabManager.CurrentFocusColumnIndex]; }
            set
            {
                var previous = FocusedColumn;
                TabManager.CurrentFocusColumnIndex = _oldFocus = _columns.IndexOf(value);
                previous.UpdateFocus();
                value.UpdateFocus();
                RaisePropertyChanged();
            }
        }

        private void UpdateFocusFromModel(int newFocus)
        {
            DispatcherHolder.Enqueue(() =>
            {
                if (newFocus == _oldFocus) return;
                _columns[_oldFocus].UpdateFocus();
                _columns[newFocus].UpdateFocus();
                _oldFocus = newFocus;
                RaisePropertyChanged(() => FocusedColumn);
            });
        }

        public void CloseTab(ColumnViewModel column, TabViewModel tab)
        {
            var ci = _columns.IndexOf(column);
            var ti = column.Tabs.IndexOf(tab);
            TabManager.CloseTab(ci, ti);
        }

        void MainWindowModel_OnTimelineFocusRequested(TimelineFocusRequest req)
        {
            switch (req)
            {
                case TimelineFocusRequest.TopOfTimeline:
                    ExecuteTimelineAction(tlvm =>
                    {
                        if (tlvm.Timeline.Count >= 0)
                        {
                            tlvm.FocusedStatus = tlvm.Timeline[0];
                        }
                    });
                    break;
                case TimelineFocusRequest.BottomOfTimeline:
                    ExecuteTimelineAction(tlvm =>
                    {
                        if (tlvm.Timeline.Count >= 0)
                        {
                            tlvm.FocusedStatus = tlvm.Timeline[tlvm.Timeline.Count - 1];
                        }
                    });
                    break;
                case TimelineFocusRequest.AboveStatus:
                    ExecuteTimelineAction(tlvm =>
                    {
                        if (tlvm.Timeline.Count == 0 || tlvm.FocusedStatus == null) return;
                        var index = tlvm.Timeline.IndexOf(tlvm.FocusedStatus) - 1;
                        tlvm.FocusedStatus = index < 0 ? null : tlvm.Timeline[index];
                    });
                    break;
                case TimelineFocusRequest.BelowStatus:
                    ExecuteTimelineAction(tlvm =>
                    {
                        if (tlvm.Timeline.Count == 0 || tlvm.FocusedStatus == null) return;
                        var index = tlvm.Timeline.IndexOf(tlvm.FocusedStatus) + 1;
                        if (index < tlvm.Timeline.Count)
                        {
                            tlvm.FocusedStatus = tlvm.Timeline[index];
                        }
                    });
                    break;
            }
        }

        #region Key assign control

        private static bool _isRegisteredEvents;
        private void RegisterEvents()
        {
            if (_isRegisteredEvents) throw new InvalidOperationException();
            _isRegisteredEvents = true;
            MainWindowModel.OnTimelineFocusRequested += MainWindowModel_OnTimelineFocusRequested;
            // Timeline actions
            KeyAssignManager.RegisterActions(
                KeyAssignAction.Create("CreateTab", () => FocusedColumn.CreateNewTab()),
                KeyAssignAction.Create("ToggleFocus", () => ExecuteStatusAction(s => s.ToggleFocus())),
                KeyAssignAction.Create("ToggleSelect", () => ExecuteStatusAction(s => s.ToggleSelect())),
                KeyAssignAction.Create("ClearSelect", () => ExecuteTimelineAction(t => t.DeselectAll())),
                KeyAssignAction.Create("Reply", () => ExecuteStatusAction(s => s.Reply())),
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
                KeyAssignAction.Create("OpenUserWeb", () => ExecuteStatusAction(s => s.OpenUserWeb())),
                KeyAssignAction.Create("OpenUserFavstar", () => ExecuteStatusAction(s => s.OpenUserFavstar())),
                KeyAssignAction.Create("OpenUserTwilog", () => ExecuteStatusAction(s => s.OpenUserTwilog())),
                KeyAssignAction.Create("OpenSource", () => ExecuteStatusAction(s => s.OpenSourceLink())),
                KeyAssignAction.Create("OpenThumbnail", () => ExecuteStatusAction(s => s.OpenFirstImage())),
                KeyAssignAction.Create("OpenConversation", () => ExecuteStatusAction(s => s.ShowConversation())),
                KeyAssignAction.Create("MuteUser", () => ExecuteStatusAction(s => s.MuteUser())),
                KeyAssignAction.Create("MuteClient", () => ExecuteStatusAction(s => s.MuteClient())),
                KeyAssignAction.Create("ReportAsSpam", () => ExecuteStatusAction(s => s.ReportAsSpam())),
                KeyAssignAction.Create("GiveTrophy", () => ExecuteStatusAction(s => s.GiveFavstarTrophy()))
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

        internal static TimelineViewModelBase TimelineActionTargetOverride { get; set; }

        #endregion

    }
}
