using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Livet.Messaging;
using StarryEyes.Breezy.Api.Rest;
using StarryEyes.Breezy.DataModel;
using StarryEyes.Models;
using StarryEyes.Models.Backstages.NotificationEvents;
using StarryEyes.Models.Stores;
using StarryEyes.Models.Tab;
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
            this._timelineModel = new TimelineModel(
                _ => false,
                (id, c, _) =>
                {
                    var info = AccountsStore.Accounts
                                            .Shuffle()
                                            .Select(s => s.AuthenticateInfo)
                                            .FirstOrDefault();
                    if (info == null)
                    {
                        return Observable.Empty<TwitterStatus>();
                    }
                    return info.GetFavorites(this._parent.User.User.Id, max_id: id, count: c)
                               .OrderByDescending(s => s.CreatedAt)
                               .Catch((Exception ex) =>
                               {
                                   BackstageModel.RegisterEvent(new OperationFailedEvent(ex.Message));
                                   return Observable.Empty<TwitterStatus>();
                               });
                });
            this.CompositeDisposable.Add(_timelineModel);
            IsLoading = true;
            _timelineModel.ReadMore(null)
                          .Finally(() => IsLoading = false)
                          .Subscribe();
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