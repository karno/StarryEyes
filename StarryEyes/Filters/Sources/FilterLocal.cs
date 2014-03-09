using System;
using System.Linq;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Filters.Expressions;
using StarryEyes.Models.Timelines.Tabs;

namespace StarryEyes.Filters.Sources
{
    /// <summary>
    /// General filter.
    /// </summary>
    public class FilterLocal : FilterSourceBase
    {
        private readonly string _tabName;

        private TabModel _tab;

        public FilterLocal() { }

        public FilterLocal(string tabName)
        {
            this._tabName = tabName;
            this.ResolveTabReference();
        }

        public override Func<TwitterStatus, bool> GetEvaluator()
        {
            this.ResolveTabReference();
            return this._tab == null || this._tab.FilterQuery == null
                ? FilterExpressionBase.Tautology
                : this._tab.FilterQuery.GetEvaluator();
        }

        public override string GetSqlQuery()
        {
            this.ResolveTabReference();
            return this._tab == null || this._tab.FilterQuery == null
                ? "1"
                : this._tab.FilterQuery.GetSqlQuery();
        }

        public override string FilterKey
        {
            get { return "local"; }
        }

        public override string FilterValue
        {
            get { return _tabName; }
        }

        private void ResolveTabReference()
        {
            if (string.IsNullOrEmpty(_tabName))
            {
                return;
            }

            if (_tab == null)
            {
                if (!TabManager.GetColumnInfoData().Any())
                {
                    // tabs are initializing, skip
                    return;
                }

                _tab = TabManager.GetColumnInfoData()
                    .SelectMany(c => c.Tabs)
                    .FirstOrDefault(t => t.Name == _tabName);

                if (this._tab == null)
                {
                    throw new ArgumentException("指定されたタブが見つかりませんでした。");
                }
            }

            if (EnumerableEx.Return(this)
                .Expand(f => f._tab != null && f._tab.FilterQuery != null
                    ? f._tab.FilterQuery.Sources.OfType<FilterLocal>()
                    : Enumerable.Empty<FilterLocal>()
                )
                .Skip(1)
                .Any(f => f._tab == _tab)
            )
            {
                throw new ArgumentException("タブの参照が循環しています。");
            }
        }
    }
}
