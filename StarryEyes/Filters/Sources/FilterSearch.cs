using System;
using System.Linq;
using System.Reactive.Linq;
using StarryEyes.Models.Connection.Extend;
using StarryEyes.Settings;
using StarryEyes.Breezy.Api.Rest;
using StarryEyes.Breezy.DataModel;

namespace StarryEyes.Filters.Sources
{
    public class FilterSearch : FilterSourceBase
    {
        private string _query;
        public FilterSearch(string query)
        {
            this._query = query;
        }

        public override Func<TwitterStatus, bool> GetEvaluator()
        {
            return _ => _.Text.IndexOf(_query) >= 0;
        }

        protected override IObservable<TwitterStatus> ReceiveSink(long? max_id)
        {
            return Observable.Defer(() => Setting.Accounts.Shuffle().Take(1).ToObservable())
                .SelectMany(a => a.AuthenticateInfo.SearchTweets(_query));
        }

        private bool _isActivated = false;
        public override void Activate()
        {
            if (_isActivated) return;
            _isActivated = true;
            SearchReceiver.RegisterSearchQuery(_query);
        }

        public override void Deactivate()
        {
            if (!_isActivated) return;
            _isActivated = false;
            SearchReceiver.RemoveSearchQuery(_query);
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
