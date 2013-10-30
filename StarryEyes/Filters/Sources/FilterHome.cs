using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.TwitterApi.Rest;
using StarryEyes.Anomaly.Utils;
using StarryEyes.Models.Accounting;

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

        public override string GetSqlQuery()
        {
            var accounts = GetAccountsFromString(_screenName).Memoize();
            var ads = accounts.Select(a => a.Id.ToString(CultureInfo.InvariantCulture))
                              .JoinString(",");
            var userMention = ((int)EntityType.UserMentions).ToString(CultureInfo.InvariantCulture);
            var followings = accounts.SelectMany(a => a.RelationData.Followings)
                                     .Select(id => id.ToString(CultureInfo.InvariantCulture))
                                     .JoinString(",");
            return "(UserId in (" + ads + ") OR " +
                   "UserId in (" + followings + ") OR " +
                   "Id in (select ParentId from StatusEntity where " +
                   "EntityType = " + userMention + " and " +
                   "UserId in (" + ads + "))";
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
                             .SelectMany(a => a.GetHomeTimelineAsync(count: 50, maxId: maxId).ToObservable());
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
