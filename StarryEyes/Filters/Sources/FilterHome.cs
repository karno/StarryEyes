using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.TwitterApi.Rest;
using StarryEyes.Anomaly.Utils;
using StarryEyes.Models.Accounting;
using StarryEyes.Settings;

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
            var ads = GetAccountsFromString(_screenName);
            return _ => CheckVisibleTimeline(_, ads);
        }

        private bool CheckVisibleTimeline(TwitterStatus status, IEnumerable<TwitterAccount> datas)
        {
            if (status.StatusType == StatusType.DirectMessage)
                return false;
            return datas.Any(account =>
                             status.User.Id == account.Id ||
                             account.RelationData.IsFollowing(status.User.Id) ||
                             FilterSystemUtil.InReplyToUsers(status).Contains(account.Id));
        }

        protected override IObservable<TwitterStatus> ReceiveSink(long? maxId)
        {
            return Observable.Defer(() => GetAccountsFromString(_screenName).ToObservable())
                             .SelectMany(a => a.GetHomeTimeline(count: 50, maxId: maxId).ToObservable());
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
