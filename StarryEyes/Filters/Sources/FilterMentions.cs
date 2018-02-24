using System;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using Cadena.Api.Rest;
using Cadena.Data;
using StarryEyes.Casket.DatabaseModels;

namespace StarryEyes.Filters.Sources
{
    public class FilterMentions : FilterSourceBase
    {
        private readonly string _screenName;

        public FilterMentions()
        {
        }

        public FilterMentions(string screenName)
        {
            _screenName = screenName;
        }

        public override Func<TwitterStatus, bool> GetEvaluator()
        {
            var ads = GetAccountsFromString(_screenName);
            return _ => _.StatusType == StatusType.Tweet &&
                        ads.Any(ad => FilterSystemUtil.InReplyToUsers(_).Contains(ad.Id));
        }

        public override string GetSqlQuery()
        {
            var type = ((int)StatusType.Tweet).ToString(CultureInfo.InvariantCulture);
            var userMention = ((int)EntityType.UserMentions).ToString(CultureInfo.InvariantCulture);
            var ads = GetAccountsFromString(_screenName)
                .Select(a => a.Id.ToString(CultureInfo.InvariantCulture))
                .JoinString(",");
            return "StatusType = " + type + " and " +
                   "Id in (select ParentId from StatusEntity where " +
                   "EntityType = " + userMention + " and " +
                   "UserId in (" + ads + "))";
        }

        protected override IObservable<TwitterStatus> ReceiveSink(long? maxId)
        {
            return Observable.Defer(() => GetAccountsFromString(_screenName).ToObservable())
                             .SelectMany(a => a
                                 .CreateAccessor()
                                 .GetMentionsAsync(50, null, includeRetweets: false, maxId: maxId,
                                     cancellationToken: CancellationToken.None).ToObservable())
                             .SelectMany(o => o.Result);
        }

        public override string FilterKey => "mentions";

        public override string FilterValue => _screenName;
    }
}