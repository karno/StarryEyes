using System;
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
                    MainAreaModel.Columns,
                    cm => new ColumnViewModel(this, cm),
                    DispatcherHelper.UIDispatcher));
            CompositeDisposable.Add(
                Observable.FromEvent(
                    h => MainAreaModel.OnCurrentFocusColumnChanged += h,
                    h => MainAreaModel.OnCurrentFocusColumnChanged -= h)
                          .Select(_ => MainAreaModel.CurrentFocusColumnIndex)
                          .Subscribe(UpdateFocusFromModel));
            RegisterEvents();
        }

        public ReadOnlyDispatcherCollectionRx<ColumnViewModel> Columns
        {
            get { return _columns; }
        }

        private int _oldFocus;
        public ColumnViewModel FocusedColumn
        {
            get { return _columns[MainAreaModel.CurrentFocusColumnIndex]; }
            set
            {
                var previous = FocusedColumn;
                MainAreaModel.CurrentFocusColumnIndex = _oldFocus = _columns.IndexOf(value);
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
            MainAreaModel.CloseTab(ci, ti);
        }

        private static bool _isRegisteredEvent;
        private void RegisterEvents()
        {
            if (_isRegisteredEvent) throw new InvalidOperationException();
            _isRegisteredEvent = true;
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
                KeyAssignAction.Create("OpenWeb", () => ExecuteStatusAction(s => s.OpenWeb())),
                KeyAssignAction.Create("OpenFavstar", () => ExecuteStatusAction(s => s.OpenFavstar())),
                KeyAssignAction.Create("OpenUserWeb", () => ExecuteStatusAction(s => s.OpenUserWeb())),
                KeyAssignAction.Create("OpenUserFavstar", () => ExecuteStatusAction(s => s.OpenUserFavstar())),
                KeyAssignAction.Create("OpenUserTwilog", () => ExecuteStatusAction(s => s.OpenUserTwilog())),
                KeyAssignAction.Create("OpenSource", () => ExecuteStatusAction(s => s.OpenSourceLink())),
                KeyAssignAction.Create("OpenThumbnail", () => ExecuteStatusAction(s => s.OpenFirstImage())),
                KeyAssignAction.Create("OpenConversation", () => ExecuteStatusAction(s => s.ShowConversation())),
                KeyAssignAction.Create("MuteKeyword", () => ExecuteStatusAction(s => s.MuteKeyword())),
                KeyAssignAction.Create("MuteUser", () => ExecuteStatusAction(s => s.MuteUser())),
                KeyAssignAction.Create("MuteClient", () => ExecuteStatusAction(s => s.MuteClient())),
                KeyAssignAction.Create("ReportAsSpam", () => ExecuteStatusAction(s => s.ReportAsSpam())),
                KeyAssignAction.Create("GiveTrophy", () => ExecuteStatusAction(s => s.GiveFavstarTrophy()))
                );

            // Timeline argumentable actions
            // reply, favorite, retweet, quote
            // TODO

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

        internal void ExecuteTimelineAction(Action<TimelineViewModelBase> action)
        {
            var timeline = TimelineActionHijacker ?? FocusedColumn.FocusedTab;
            if (timeline == null) return;
            DispatcherHolder.Enqueue(() => action(timeline));
        }

        internal void ExecuteStatusAction(Action<StatusViewModel> status)
        {
            var timeline = TimelineActionHijacker ?? FocusedColumn.FocusedTab;
            StatusViewModel target;
            if (timeline == null || (target = timeline.FocusedStatus) == null) return;
            status(target);
        }

        internal static TimelineViewModelBase TimelineActionHijacker { get; set; }
    }
}
