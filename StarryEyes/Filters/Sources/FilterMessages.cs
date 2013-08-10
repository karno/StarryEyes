using System;
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
            return _ => ads.Any(ad => FilterSystemUtil.InReplyToUsers(_).Contains(ad.Id));
        }

        protected override IObservable<TwitterStatus> ReceiveSink(long? maxId)
        {
            return Observable.Defer(() => GetAccountsFromString(_screenName).ToObservable())
                .SelectMany(a => a.GetDirectMessages(count: 50, maxId: maxId).ToObservable());
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
