using System;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.TwitterApi.Rest;
using StarryEyes.Anomaly.Utils;

namespace StarryEyes.Filters.Sources
{
    public class FilterMessages : FilterSourceBase
    {
        private readonly string _screenName;
        public FilterMessages() { }

        public FilterMessages(string screenName)
        {
            this._screenName = screenName;
        }

        public override Func<TwitterStatus, bool> GetEvaluator()
        {
            var ads = GetAccountsFromString(_screenName);
            return _ => _.StatusType == StatusType.DirectMessage &&
                        ads.Any(ad => FilterSystemUtil.InReplyToUsers(_).Contains(ad.Id));
        }

        public override string GetSqlQuery()
        {
            var type = ((int)StatusType.DirectMessage).ToString(CultureInfo.InvariantCulture);
            var aids = GetAccountsFromString(_screenName)
                .Select(a => a.Id.ToString(CultureInfo.InvariantCulture))
                .JoinString(",");
            return "StatusType = " + type + " and " +
                   "InReplyToOrRecipientUserId IN (" + aids + ")";
        }

        protected override IObservable<TwitterStatus> ReceiveSink(long? maxId)
        {
            return Observable.Defer(() => GetAccountsFromString(_screenName).ToObservable())
                .SelectMany(a => a.GetDirectMessagesAsync(count: 50, maxId: maxId).ToObservable());
        }

        public override string FilterKey
        {
            get { return "messages"; }
        }

        public override string FilterValue
        {
            get { return _screenName; }
        }
    }
}
