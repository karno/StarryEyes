using System;
using System.Linq;
using System.Reactive.Linq;
using StarryEyes.Models.Stores;
using StarryEyes.Breezy.Api.Rest;
using StarryEyes.Breezy.DataModel;

namespace StarryEyes.Filters.Sources
{
    public class FilterMessages : FilterSourceBase
    {
        private string _screenName;
        public FilterMessages() { }

        public FilterMessages(string screenName)
        {
            this._screenName = screenName;
        }

        public override Func<TwitterStatus, bool> GetEvaluator()
        {
            var ads = GetAccountsFromString(_screenName)
                .Select(a => AccountRelationDataStore.GetAccountData(a.Id));
            return _ => ads.Any(ad => FilterSystemUtil.InReplyToUsers(_).Contains(ad.AccountId));
        }

        protected override IObservable<TwitterStatus> ReceiveSink(long? max_id)
        {
            return Observable.Defer(() => GetAccountsFromString(_screenName).ToObservable())
                .SelectMany(a => a.GetDirectMessages(count: 50, max_id: max_id));
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
