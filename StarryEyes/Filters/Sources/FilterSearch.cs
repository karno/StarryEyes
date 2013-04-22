using System;
using System.Linq;
using System.Reactive.Linq;
using StarryEyes.Breezy.Api.Rest;
using StarryEyes.Breezy.DataModel;
using StarryEyes.Models.Receivers;
using StarryEyes.Models.Stores;

namespace StarryEyes.Filters.Sources
{
    public class FilterSearch : FilterSourceBase
    {
        private readonly string _query;
        public FilterSearch(string query)
        {
            this._query = query;
        }

        public override Func<TwitterStatus, bool> GetEvaluator()
        {
            return _ => _.Text.IndexOf(_query) >= 0;
        }

        protected override IObservable<TwitterStatus> ReceiveSink(long? maxId)
        {
            return Observable.Defer(() => AccountsStore.Accounts.Shuffle().Take(1).ToObservable())
                .SelectMany(a => a.AuthenticateInfo.SearchTweets(_query));
        }

        private bool _isActivated;
        public override void Activate()
        {
            if (_isActivated) return;
            _isActivated = true;
            ReceiversManager.RegisterSearchQuery(_query);
        }

        public override void Deactivate()
        {
            if (!_isActivated) return;
            _isActivated = false;
            ReceiversManager.UnregisterSearchQuery(_query);
        }

        public override string FilterKey
        {
            get { return "search"; }
        }

        public override string FilterValue
        {
            get { return _query; }
        }
    }
}
