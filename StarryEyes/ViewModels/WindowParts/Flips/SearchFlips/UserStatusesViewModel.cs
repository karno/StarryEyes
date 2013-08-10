using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Livet.Messaging;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.TwitterApi.Rest;
using StarryEyes.Models.Tab;
using StarryEyes.Settings;
using StarryEyes.ViewModels.WindowParts.Timelines;

namespace StarryEyes.ViewModels.WindowParts.Flips.SearchFlips
{
    public class UserStatusesViewModel : TimelineViewModelBase
    {
        private readonly UserInfoViewModel _parent;

        private readonly TimelineModel _timelineModel;
        protected override TimelineModel TimelineModel
        {
            get { return _timelineModel; }
        }

        protected override IEnumerable<long> CurrentAccounts
        {
            get
            {
                var ctab = TabManager.CurrentFocusTab;
                return ctab != null ? ctab.BindingAccountIds : Enumerable.Empty<long>();
            }
        }

        public UserStatusesViewModel(UserInfoViewModel parent)
        {
            _parent = parent;
            _timelineModel = TimelineModel.FromAsyncTask(
                s => s.User.Id == parent.User.User.Id,
                async (id, c, _) =>
                {
                    var account =
                        Setting.Accounts.Collection.FirstOrDefault(
                            a => a.RelationData.IsFollowing(this._parent.User.User.Id)) ??
                        Setting.Accounts.GetRandomOne();
                    if (account != null)
                    {
                        return await account.GetUserTimeline(this._parent.User.User.Id, maxId: id, count: c);
                    }
                    return Enumerable.Empty<TwitterStatus>();
                });
            this.CompositeDisposable.Add(_timelineModel);
            IsLoading = true;
            Task.Run(async () =>
            {
                await _timelineModel.ReadMore(null);
                IsLoading = false;
            });
        }

        public override void GotPhysicalFocus()
        {
            MainAreaViewModel.TimelineActionTargetOverride = this;
        }

        public void SetPhysicalFocus()
        {
            this.Messenger.Raise(new InteractionMessage("SetPhysicalFocus"));
        }

        public void PinToTab()
        {
            TabManager.CreateTab(
                new TabModel(_parent.ScreenName, "from local where user == \"" + _parent.ScreenName + "\""));
        }
    }
}
