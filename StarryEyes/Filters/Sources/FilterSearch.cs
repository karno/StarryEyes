using System;
using System.Collections.Generic;
using System.Linq;
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
            var pan = SplitPositiveNegativeQuery(this._query);
            var positive = pan.Item1;
            var negative = pan.Item2;
            return status =>
                   positive.All(p => status.Text.IndexOf(p, StringComparison.CurrentCultureIgnoreCase) >= 0) &&
                   negative.All(p => status.Text.IndexOf(p, StringComparison.CurrentCultureIgnoreCase) == -1);
        }

        public override string GetSqlQuery()
        {
            var pan = SplitPositiveNegativeQuery(this._query);
            return Enumerable.Concat(
                pan.Item1.Select(q => "Text like '%" + q + "%'"),
                pan.Item2.Select(q => "Text not like '%" + q + "%'"))
                             .JoinString(" and ");
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

        public static Tuple<IEnumerable<string>, IEnumerable<string>> SplitPositiveNegativeQuery(string query)
        {

            var splitted = query.Split(new[] { " ", "\t", "　" },
                                       StringSplitOptions.RemoveEmptyEntries)
                                .Distinct().ToArray();
            var positive = splitted.Where(s => !s.StartsWith("-")).ToArray();
            var negative = splitted.Where(s => s.StartsWith("-")).Select(s => s.Substring(1)).ToArray();
            return Tuple.Create(positive.AsEnumerable(), negative.AsEnumerable());
        }
    }
}
