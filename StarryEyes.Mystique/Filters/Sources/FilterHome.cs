using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using StarryEyes.Mystique.Models.Common;
using StarryEyes.Mystique.Models.Store;
using StarryEyes.SweetLady.Api.Rest;
using StarryEyes.SweetLady.DataModel;

namespace StarryEyes.Mystique.Filters.Sources
{
    public class FilterHome : FilterSourceBase
    {
        private string _screenName;
        public FilterHome() { }

        public FilterHome(string screenName)
        {
            this._screenName = screenName;
        }

        public override Func<TwitterStatus, bool> GetEvaluator()
        {
            var ads = GetAccountsFromString(_screenName)
                .Select(a => AccountDataStore.GetAccountData(a.Id));
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

        protected override IObservable<TwitterStatus> ReceiveSink(long? max_id)
        {
            return Observable.Defer(() => GetAccountsFromString(_screenName).ToObservable())
                .SelectMany(a => a.GetHomeTimeline(count: 50, max_id: max_id));
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
