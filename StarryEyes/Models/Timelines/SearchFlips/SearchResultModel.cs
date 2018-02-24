using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using Cadena.Api.Parameters;
using Cadena.Api.Rest;
using Cadena.Data;
using StarryEyes.Filters;
using StarryEyes.Filters.Expressions;
using StarryEyes.Filters.Parsing;
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

        public string Query => _query;

        public SearchResultModel(string query, SearchOption option)
        {
            _query = query;
            _option = option;
            if (option != SearchOption.Web)
            {
                IsSubscribeBroadcaster = true;
            }
            PrepareFilter();
        }

        private void PrepareFilter()
        {
            var disposable = new CompositeDisposable();
            var prev = Interlocked.Exchange(ref _previousFilterListener, disposable);
            prev?.Dispose();
            switch (_option)
            {
                case SearchOption.Web:
                    _filterFunc = FilterExpressionBase.Tautology;
                    break;
                case SearchOption.Query:
                    try
                    {
                        var fq = QueryCompiler.Compile(_query);
                        _filterQuery = fq;
                        fq.Activate();
                        disposable.Add(Disposable.Create(fq.Deactivate));
                        disposable.Add(Observable.FromEvent(
                                                     h => fq.InvalidateRequired += h,
                                                     h => fq.InvalidateRequired -= h)
                                                 .Subscribe(r => QueueInvalidateTimeline()));
                    }
                    catch
                    {
                        _filterQuery = null;
                        _filterFunc = FilterExpressionBase.Contradiction;
                        _filterSql = FilterExpressionBase.ContradictionSql;
                    }
                    break;
                default:
                    var splitted = _query.Split(new[] { " ", "\t", "　" },
                                             StringSplitOptions.RemoveEmptyEntries)
                                         .Distinct().ToArray();
                    var positive = splitted.Where(s => !s.StartsWith("-")).ToArray();
                    var negative = splitted.Where(s => s.StartsWith("-")).Select(s => s.Substring(1)).ToArray();
                    var filter = new Func<TwitterStatus, bool>(
                        status =>
                            positive.Any(s => status.GetEntityAidedText(EntityDisplayMode.LinkUri)
                                                    .IndexOf(s, StringComparison.CurrentCultureIgnoreCase) >= 0) &&
                            !negative.Any(s => status.GetEntityAidedText(EntityDisplayMode.LinkUri)
                                                     .IndexOf(s, StringComparison.CurrentCultureIgnoreCase) >= 0));
                    var psql = positive.Select(s => "LOWER(EntityAidedText) like LOWER('%" + s + "%')")
                                       .JoinString(" OR ");
                    var nsql = negative.Select(s => "LOWER(EntityAidedText) not like LOWER('%" + s + "%')")
                                       .JoinString(" AND ");
                    var sql = psql.SqlConcatAnd(nsql);
                    var ctab = TabManager.CurrentFocusTab;
                    var ctf = ctab?.FilterQuery;
                    if (_option == SearchOption.CurrentTab && ctf != null)
                    {
                        // add current tab filter
                        _filterSql = sql.SqlConcatAnd(ctf.GetSqlQuery());
                        var func = ctf.GetEvaluator();
                        _filterFunc = s => func(s) && filter(s);
                    }
                    else
                    {
                        _filterSql = sql;
                        _filterFunc = filter;
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
                _filterFunc = _filterQuery.GetEvaluator();
                _filterSql = _filterQuery.GetSqlQuery();
                return !_filterQuery.IsPreparing;
            }
            return true;
        }

        protected override bool CheckAcceptStatusCore(TwitterStatus status)
        {
            return _filterFunc(status);
        }

        protected override IObservable<TwitterStatus> Fetch(long? maxId, int? count)
        {
            if (_option == SearchOption.Web)
            {
                var acc = Setting.Accounts.GetRandomOne();
                if (acc == null) return Observable.Empty<TwitterStatus>();
                System.Diagnostics.Debug.WriteLine("SEARCHPANE SEARCH QUERY: " + _query);
                var param = new SearchParameter(_query, maxId: maxId, count: count,
                    lang: String.IsNullOrWhiteSpace(Setting.SearchLanguage.Value)
                        ? null
                        : Setting.SearchLanguage.Value,
                    locale: String.IsNullOrWhiteSpace(Setting.SearchLocale.Value)
                        ? null
                        : Setting.SearchLocale.Value);
                return acc.CreateAccessor()
                          .SearchAsync(param, CancellationToken.None)
                          .ToObservable()
                          .SelectMany(s => s.Result)
                          .Do(StatusInbox.Enqueue);
            }
            return StatusProxy.FetchStatuses(_filterFunc, _filterSql, maxId, count)
                              .ToObservable()
                              .SelectMany(s => s)
                              .Merge(_filterQuery != null
                                  ? _filterQuery.ReceiveSources(maxId)
                                  : Observable.Empty<TwitterStatus>());
        }

        public string CreateFilterQuery()
        {
            switch (_option)
            {
                case SearchOption.Local:
                case SearchOption.CurrentTab:
                    var pan = SplitPositiveNegativeQuery(_query);
                    var query = pan.Item1.Select(s => "text contains " + s.EscapeForQuery().Quote())
                                   .Concat(
                                       pan.Item2.Select(s => "!(text contains " + s.EscapeForQuery().Quote() + ")"))
                                   .JoinString("&&");
                    var ctab = TabManager.CurrentFocusTab;
                    var ctf = ctab?.FilterQuery;
                    if (_option != SearchOption.CurrentTab || ctf == null)
                    {
                        return "where " + query;
                    }
                    var cqf = QueryCompiler.CompileFilters(query);
                    var filters = ctf.PredicateTreeRoot.Operator;
                    var nfq = new FilterQuery
                    {
                        Sources = ctf.Sources.ToArray(),
                        PredicateTreeRoot = new FilterExpressionRoot
                        {
                            Operator = filters.And(cqf.Operator)
                        }
                    };
                    return nfq.ToQuery();
                case SearchOption.Query:
                    return _filterQuery == null ? "!()" : _query;
                case SearchOption.Web:
                    return "from search:" + _query.EscapeForQuery().Quote() + " where ()";
                default:
                    throw new ArgumentOutOfRangeException();
            }
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