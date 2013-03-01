using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using StarryEyes.Breezy.Api.Rest;
using StarryEyes.Breezy.DataModel;
using StarryEyes.Models.Stores;

namespace StarryEyes.Filters.Sources
{
    public class FilterHome : FilterSourceBase
    {
        private readonly string _screenName;
        public FilterHome() { }

        public FilterHome(string screenName)
        {
            this._screenName = screenName;
        }

        public override Func<TwitterStatus, bool> GetEvaluator()
        {
            var ads = GetAccountsFromString(_screenName)
                .Select(a => AccountRelationDataStore.GetAccountData(a.Id));
            return _ => CheckVisibleTimeline(_, ads);
        }

        private bool CheckVisibleTimeline(TwitterStatus status, IEnumerable<AccountData> datas)
        {
            if (status.StatusType == StatusType.DirectMessage)
                return false;
            return datas.Any(ad =>
                status.User.Id == ad.AccountId ||
                ad.IsFollowing(status.User.Id) ||
                FilterSystemUtil.InReplyToUsers(status).Contains(ad.AccountId));
        }

        protected override IObservable<TwitterStatus> ReceiveSink(long? maxId)
        {
            return Observable.Defer(() => GetAccountsFromString(_screenName).ToObservable())
                .SelectMany(a => a.GetHomeTimeline(count: 50, max_id: maxId));
        }

        public override string FilterKey
        {
            get { return "home"; }
        }

        public override string FilterValue
        {
            get { return _screenName; }
        }
    }
}
