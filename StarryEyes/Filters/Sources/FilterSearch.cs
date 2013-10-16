using System;
using System.Reactive.Linq;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.TwitterApi.Rest;
using StarryEyes.Anomaly.Utils;
using StarryEyes.Models.Receiving;
using StarryEyes.Settings;

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
            return _ => _.Text.IndexOf(this._query, StringComparison.Ordinal) >= 0;
        }

        public override string GetSqlQuery()
        {
            return "Text like '%" + _query + "%'";
        }

        protected override IObservable<TwitterStatus> ReceiveSink(long? maxId)
        {
            return Observable.Start(() => Setting.Accounts.GetRandomOne())
                             .Where(a => a != null)
                             .SelectMany(a => a.SearchAsync(_query, maxId: maxId).ToObservable());
        }

        private bool _isActivated;
        public override void Activate()
        {
            if (_isActivated) return;
            _isActivated = true;
            ReceiveManager.RegisterSearchQuery(_query);
        }

        public override void Deactivate()
        {
            if (!_isActivated) return;
            _isActivated = false;
            ReceiveManager.UnregisterSearchQuery(_query);
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
