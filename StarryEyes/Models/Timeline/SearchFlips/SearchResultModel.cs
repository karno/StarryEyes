using System;
using System.Linq;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.TwitterApi.Rest;
using StarryEyes.Anomaly.Utils;
using StarryEyes.Filters.Expressions;
using StarryEyes.Filters.Parsing;
using StarryEyes.Models.Databases;
using StarryEyes.Settings;

namespace StarryEyes.Models.Timeline.SearchFlips
{
    public class SearchResultModel : TimelineModelBase
    {
        private readonly string _query;
        private readonly SearchOption _option;

        private string _filterSql = FilterExpressionBase.ContradictionSql;
        private Func<TwitterStatus, bool> _filterFunc = FilterExpressionBase.Contradiction;

        public SearchResultModel(string query, SearchOption option)
        {
            _query = query;
            _option = option;
            PrepareSearchPredicate();
            if (option != SearchOption.Web)
            {
                IsSubscribeBroadcaster = true;
            }
        }

        private void PrepareSearchPredicate()
        {
            switch (_option)
            {
                case SearchOption.Web:
                    _filterFunc = FilterExpressionBase.Tautology;
                    break;
                case SearchOption.Query:
                    try
                    {
                        var fq = QueryCompiler.Compile(_query);
                        _filterFunc = fq.GetEvaluator();
                        _filterSql = fq.GetSqlQuery();
                    }
                    catch
                    {
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
                    _filterFunc =
                        status =>
                        positive.Any(s => status.Text.IndexOf(s, StringComparison.CurrentCultureIgnoreCase) >= 0) &&
                        !negative.Any(s => status.Text.IndexOf(s, StringComparison.CurrentCultureIgnoreCase) >= 0);
                    var psql = positive.Select(s => "TEXT like '%" + s + "%'").JoinString(" OR ");
                    var nsql = negative.Select(s => "TEXT not like '%" + s + "%'").JoinString(" AND ");
                    if (!String.IsNullOrEmpty(psql) && !String.IsNullOrEmpty(nsql))
                    {
                        psql = "(" + psql + ")";
                    }
                    _filterSql = psql.SqlConcatAnd(nsql);
                    break;
            }
        }

        protected override void PreInvalidateTimeline()
        {
            PrepareSearchPredicate();
        }

        protected override bool CheckAcceptStatus(TwitterStatus status)
        {
            return _filterFunc(status);
        }

        protected override IObservable<TwitterStatus> Fetch(long? maxId, int? count)
        {
            switch (_option)
            {
                case SearchOption.None:
                case SearchOption.Quick:
                case SearchOption.Query:
                    return StatusProxy.FetchStatuses(_filterSql, maxId, count);
                case SearchOption.Web:
                    return Setting.Accounts
                                  .GetRandomOne()
                                  .SearchAsync(_query, maxId: maxId, count: count)
                                  .ToObservable();
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
        None,
        /// <summary>
        /// Search from tabs only
        /// </summary>
        Quick,
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
