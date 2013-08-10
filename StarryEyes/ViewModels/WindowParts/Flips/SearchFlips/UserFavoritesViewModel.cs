using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Livet.Messaging;
using StarryEyes.Anomaly.TwitterApi.Rest;
using StarryEyes.Models.Tab;
using StarryEyes.Settings;
using StarryEyes.ViewModels.WindowParts.Timelines;

namespace StarryEyes.ViewModels.WindowParts.Flips.SearchFlips
{
    public class UserFavoritesViewModel : TimelineViewModelBase
    {
        private readonly UserInfoViewModel _parent;

        private readonly TimelineModel _timelineModel;
        protected override TimelineModel TimelineModel
        {
            get { return this._timelineModel; }
        }

        protected override IEnumerable<long> CurrentAccounts
        {
            get
            {
                var ctab = TabManager.CurrentFocusTab;
                return ctab != null ? ctab.BindingAccountIds : Enumerable.Empty<long>();
            }
        }

        public UserFavoritesViewModel(UserInfoViewModel parent)
        {
            this._parent = parent;
            this._timelineModel = TimelineModel.FromAsyncTask(
                _ => false,
                async (id, c, _) =>
                {
                    var info = Setting.Accounts.Collection.FirstOrDefault(a => a.RelationData.IsFollowing(_parent.User.User.Id)) ??
                               Setting.Accounts.GetRandomOne();
                    return await info.GetFavorites(this._parent.User.User.Id, c, maxId: id);
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
                new TabModel(this._parent.ScreenName, "from local where favs contains @" + this._parent.ScreenName));
        }
    }
}