using System;
using System.Collections.Generic;
using StarryEyes.Anomaly.TwitterApi.DataModels;

namespace StarryEyes.Filters.Expressions.Operators
{
    public class FilterNegate : FilterSingleValueOperator
    {
        protected override string OperatorString
        {
            get { return "!"; }
        }

        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get { yield return FilterExpressionType.Boolean; }
        }

        public override Func<TwitterStatus, bool> GetBooleanValueProvider()
        {
            var bvp = Value.GetBooleanValueProvider();
            return _ => !bvp(_);
        }

        public override string GetBooleanSqlQuery()
        {
            return "NOT " + Value.GetBooleanSqlQuery();
        }

        public override string ToQuery()
        {
            return "!" + Value.ToQuery();
        }
    }

    /// <summary>
    /// Pseudo filter.
    /// </summary>
    public class FilterBracket : FilterSingleValueOperator
    {
        private readonly FilterOperatorBase _inner;
        public FilterBracket(FilterOperatorBase inner)
        {
            this._inner = inner;
        }

        protected override string OperatorString
        {
            get { return "()"; }
        }

        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get
            {
                return _inner == null ? new[] { FilterExpressionType.Boolean, FilterExpressionType.Set } :
                    _inner.SupportedTypes;
            }
        }

        public override Func<TwitterStatus, bool> GetBooleanValueProvider()
        {
            return _inner == null ? Tautology : _inner.GetBooleanValueProvider();
        }

        public override Func<TwitterStatus, long> GetNumericValueProvider()
        {
            if (_inner == null)
            {
                return _ => 0;
            }
            return _inner.GetNumericValueProvider();
        }

        public override Func<TwitterStatus, IReadOnlyCollection<long>> GetSetValueProvider()
        {
            return _inner == null ? (_ => new List<long>()) : _inner.GetSetValueProvider();
        }

        public override Func<TwitterStatus, string> GetStringValueProvider()
        {
            if (_inner == null)
            {
                return _ => String.Empty;
            }
            return _inner.GetStringValueProvider();
        }

        public override string GetBooleanSqlQuery()
        {
            return this._inner == null ? "1" : "(" + this._inner.GetBooleanSqlQuery() + ")";
        }

        public override string GetNumericSqlQuery()
        {
            return this._inner == null ? "1" : "(" + this._inner.GetNumericSqlQuery() + ")";
        }

        public override string GetSetSqlQuery()
        {
            return this._inner == null ? "1" : this._inner.GetSetSqlQuery();
        }

        public override string GetStringSqlQuery()
        {
            return this._inner == null ? "1" : this._inner.GetStringSqlQuery();
        }

        public override string ToQuery()
        {
            return this._inner == null ? "()" : "(" + this._inner.ToQuery() + ")";
        }
    }
}
