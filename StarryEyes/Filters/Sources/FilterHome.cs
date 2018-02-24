using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using Cadena.Api.Rest;
using Cadena.Data;
using StarryEyes.Casket.DatabaseModels;
using StarryEyes.Models.Accounting;
using StarryEyes.Models.Databases;

namespace StarryEyes.Filters.Sources
{
    public class FilterHome : FilterSourceBase
    {
        private readonly string _screenName;

        public FilterHome()
        {
        }

        public FilterHome(string screenName)
        {
            _screenName = screenName;
        }

        public override Func<TwitterStatus, bool> GetEvaluator()
        {
            var ads = GetAccountsFromString(_screenName);
            return s => CheckVisibleTimeline(s, ads);
        }

        public override string GetSqlQuery()
        {
            var accounts = GetAccountsFromString(_screenName).Memoize();
            var ads = accounts.Select(a => a.Id.ToString(CultureInfo.InvariantCulture))
                              .JoinString(",");
            var userMention = ((int)EntityType.UserMentions).ToString(CultureInfo.InvariantCulture);
            var followings = accounts.SelectMany(a => a.RelationData.Followings.Items)
                                     .Select(id => id.ToString(CultureInfo.InvariantCulture))
                                     .JoinString(",");
            var mytweets = "UserId in (" + ads + ")";
            var friends = "UserId in (" + followings + ")";
            var inReplyToMe = "Id in (select ParentId from StatusEntity where " +
                              "EntityType = " + userMention + " AND UserId in (" + ads + "))";
            var notReply = "InReplyToOrRecipientUserId = 0 ";
            var inReplyToFollowings = "InReplyToOrRecipientUserId in (" + followings + ")";
            return inReplyToMe
                .SqlConcatOr(mytweets)
                .SqlConcatOr(friends
                    .SqlConcatAnd(notReply
                        .SqlConcatOr(inReplyToFollowings)));
        }

        private bool CheckVisibleTimeline(TwitterStatus status, IEnumerable<TwitterAccount> datas)
        {
            if (status.StatusType == StatusType.DirectMessage)
                return false;
            return datas.Any(account =>
                status.User.Id == account.Id ||
                account.RelationData.Followings.Contains(status.User.Id) ||
                FilterSystemUtil.InReplyToUsers(status).Contains(account.Id));
        }

        protected override IObservable<TwitterStatus> ReceiveSink(long? maxId)
        {
            return Observable.Defer(() => GetAccountsFromString(_screenName).ToObservable())
                             .SelectMany(a => a
                                 .CreateAccessor()
                                 .GetHomeTimelineAsync(50, null, maxId,
                                     CancellationToken.None).ToObservable())
                             .SelectMany(o => o.Result);
        }

        public override string FilterKey => "home";

        public override string FilterValue => _screenName;
    }
}