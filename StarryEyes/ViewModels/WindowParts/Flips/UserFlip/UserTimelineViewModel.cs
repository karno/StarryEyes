using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using StarryEyes.Breezy.Api.Rest;
using StarryEyes.Breezy.DataModel;
using StarryEyes.Models.Stores;
using StarryEyes.Models.Tab;
using StarryEyes.Vanille.DataStore;
using StarryEyes.ViewModels.WindowParts.Timelines;

namespace StarryEyes.ViewModels.WindowParts.Flips.UserFlip
{
    public class UserTimelineViewModel : TimelineViewModelBase
    {
        private readonly TwitterUser _user;
        private readonly TimelineModel _timelineModel;

        public UserTimelineViewModel(TwitterUser user)
        {
            _user = user;
            _timelineModel = new TimelineModel(
                s => s.User == user,
                (maxId, count, _) => Fetch(maxId, count));
        }

        private IObservable<TwitterStatus> Fetch(long? maxId, int count)
        {
            var info = AccountsStore
                .Accounts
                .Shuffle()
                .Where(s =>
                {
                    var ad = AccountRelationDataStore.GetAccountData(s.UserId);
                    if (ad == null) return false;
                    return ad.Followings.Contains(_user.Id);
                })
                .Concat(AccountsStore.Accounts)
                .FirstOrDefault();
            var local = Observable.Start(() =>
                                        StatusStore.Find(s => s.User == _user,
                                                         maxId != null ? FindRange<long>.By(maxId.Value) : null, count))
                                 .SelectMany(_ => _);
            if (info != null)
            {
                local = local
                    .Merge(info.AuthenticateInfo.GetUserTimeline(user_id: _user.Id, max_id: maxId, count: count));
            }
            return local;
        }

        protected override TimelineModel TimelineModel
        {
            get { return _timelineModel; }
        }

        protected override IEnumerable<long> CurrentAccounts
        {
            get
            {
                var ctab = MainAreaModel.CurrentFocusTab;
                return ctab != null ? ctab.BindingAccountIds : Enumerable.Empty<long>();
            }
        }
    }
}
