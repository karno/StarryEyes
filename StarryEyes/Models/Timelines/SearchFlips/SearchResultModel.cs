using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.TwitterApi.Rest;
using StarryEyes.Anomaly.Utils;
using StarryEyes.Filters;
using StarryEyes.Filters.Expressions;
using StarryEyes.Filters.Parsing;
using StarryEyes.Filters.Sources;
using StarryEyes.Models.Databases;
using StarryEyes.Models.Receiving.Handling;
using StarryEyes.Models.Timelines.Tabs;
using StarryEyes.Settings;

namespace StarryEyes.Models.Timelines.SearchFlips
{
    public class SearchResultModel : TimelineModelBase
    {
        private readonly string _query;
        private readonly SearchOption _option;

        private string _filterSql = FilterExpressionBase.ContradictionSql;
        private Func<TwitterStatus, bool> _filterFunc = FilterExpressionBase.Contradiction;

        public string Query
        {
            get { return _query; }
        }

        public SearchResultModel(string query, SearchOption option)
        {
            this._query = query;
            this._option = option;
            if (option != SearchOption.Web)
            {
                this.IsSubscribeBroadcaster = true;
            }
            this.PrepareFilter();
        }

        private void PrepareFilter()
        {
            var disposable = new CompositeDisposable();
            var prev = Interlocked.Exchange(ref _previousFilterListener, disposable);
            if (prev != null)
            {
                prev.Dispose();
            }
            switch (this._option)
            {
                case SearchOption.Web:
                    this._filterFunc = FilterExpressionBase.Tautology;
                    break;
                case SearchOption.Query:
                    try
                    {
                        var fq = QueryCompiler.Compile(this._query);
                        _filterQuery = fq;
                        fq.Activate();
                        disposable.Add(Disposable.Create(fq.Deactivate));
                        disposable.Add(Observable.FromEvent(
                            h => fq.InvalidateRequired += h,
                            h => fq.InvalidateRequired -= h)
                                                 .Subscribe(r => this.QueueInvalidateTimeline()));
                    }
                    catch
                    {
                        _filterQuery = null;
                        this._filterFunc = FilterExpressionBase.Contradiction;
                        this._filterSql = FilterExpressionBase.ContradictionSql;
                    }
                    break;
                default:
                    var splitted = this._query.Split(new[] { " ", "\t", "　" },
                                       StringSplitOptions.RemoveEmptyEntries)
                                .Distinct().ToArray();
                    var positive = splitted.Where(s => !s.StartsWith("-")).ToArray();
                    var negative = splitted.Where(s => s.StartsWith("-")).Select(s => s.Substring(1)).ToArray();
                    var filter = new Func<TwitterStatus, bool>(
                        status =>
                        positive.Any(s => status.Text.IndexOf(s, StringComparison.CurrentCultureIgnoreCase) >= 0) &&
                        !negative.Any(s => status.Text.IndexOf(s, StringComparison.CurrentCultureIgnoreCase) >= 0));
                    var psql = positive.Select(s => "TEXT like '%" + s + "%'").JoinString(" OR ");
                    var nsql = negative.Select(s => "TEXT not like '%" + s + "%'").JoinString(" AND ");
                    var sql = psql.SqlConcatAnd(nsql);
                    var ctab = TabManager.CurrentFocusTab;
                    var ctf = ctab != null ? ctab.FilterQuery : null;
                    if (_option == SearchOption.CurrentTab && ctf != null)
                    {
                        // add current tab filter
                        this._filterSql = sql.SqlConcatAnd(ctf.GetSqlQuery());
                        var func = ctf.GetEvaluator();
                        this._filterFunc = s => func(s) && filter(s);
                    }
                    else
                    {
                        this._filterSql = sql;
                        this._filterFunc = filter;
                    }
                    break;
            }
        }

        private IDisposable _previousFilterListener;
        private FilterQuery _filterQuery;
        protected override bool PreInvalidateTimeline()
        {
            if (_option == SearchOption.Query && _filterQuery != null)
            {
                this._filterFunc = _filterQuery.GetEvaluator();
                this._filterSql = _filterQuery.GetSqlQuery();
                return !_filterQuery.IsPreparing;
            }
            return true;
        }

        protected override bool CheckAcceptStatus(TwitterStatus status)
        {
            return this._filterFunc(status);
        }

        protected override IObservable<TwitterStatus> Fetch(long? maxId, int? count)
        {
            if (_option == SearchOption.Web)
            {
                var acc = Setting.Accounts.GetRandomOne();
                if (acc == null) return Observable.Empty<TwitterStatus>();
                return acc.SearchAsync(this._query, maxId: maxId, count: count)
                          .ToObservable()
                          .Do(StatusInbox.Queue);
            }
            return StatusProxy.FetchStatuses(this._filterSql, maxId, count);
        }

        public string CreateFilterQuery()
        {
            switch (_option)
            {
                case SearchOption.Local:
                case SearchOption.CurrentTab:
                    var pan = FilterSearch.SplitPositiveNegativeQuery(_query);
                    var query = pan.Item1.Select(s => "text contains \"" + s + "\"")
                                   .Concat(pan.Item2.Select(s => "!(text contains \"" + s + "\")"))
                                   .JoinString("&&");
                    var ctab = TabManager.CurrentFocusTab;
                    var ctf = ctab != null ? ctab.FilterQuery : null;
                    if (_option == SearchOption.CurrentTab && ctf != null)
                    {
                        return ctf.ToQuery() + " and " + query;
                    }
                    return "where " + query;
                case SearchOption.Query:
                    return this._query;
                case SearchOption.Web:
                    return "from search:\"" + this._query + "\" where ()";
                default:
                    throw new ArgumentOutOfRangeException();
            }

        }
    }

    /// <summary>
    /// Describes searching option.
    /// </summary>
    public enum SearchOption
    {
        /// <summary>
        /// Search local store by keyword.
        /// </summary>
        Local,
        /// <summary>
        /// Search from tabs only
        /// </summary>
        CurrentTab,
        /// <summary>
        /// Search local store by query.
        /// </summary>
        Query,
        /// <summary>
        /// Search on web by keyword.
        /// </summary>
        Web
    }
}
