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
        protected override string OperatorString
        {
            get { return "()"; }
        }

        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get
            {
                return Value == null ? new[] { FilterExpressionType.Boolean, FilterExpressionType.Set } :
                    Value.SupportedTypes;
            }
        }

        public override Func<TwitterStatus, bool> GetBooleanValueProvider()
        {
            return Value == null ? Tautology : Value.GetBooleanValueProvider();
        }

        public override Func<TwitterStatus, long> GetNumericValueProvider()
        {
            if (Value == null)
            {
                return _ => 0;
            }
            return Value.GetNumericValueProvider();
        }

        public override Func<TwitterStatus, IReadOnlyCollection<long>> GetSetValueProvider()
        {
            return Value == null ? (_ => new List<long>()) : Value.GetSetValueProvider();
        }

        public override Func<TwitterStatus, string> GetStringValueProvider()
        {
            if (Value == null)
            {
                return _ => String.Empty;
            }
            return Value.GetStringValueProvider();
        }

        public override string GetBooleanSqlQuery()
        {
            return this.Value == null ? "1" : "(" + this.Value.GetBooleanSqlQuery() + ")";
        }

        public override string GetNumericSqlQuery()
        {
            return this.Value == null ? "1" : "(" + this.Value.GetNumericSqlQuery() + ")";
        }

        public override string GetSetSqlQuery()
        {
            return this.Value == null ? "1" : this.Value.GetSetSqlQuery();
        }

        public override string GetStringSqlQuery()
        {
            return this.Value == null ? "1" : this.Value.GetStringSqlQuery();
        }

        public override string ToQuery()
        {
            if (this.Value == null)
            {
                return "()";
            }
            if (this.Value is FilterBracket)
            {
                return this.Value.ToQuery();
            }
            return "(" + this.Value.ToQuery() + ")";
        }
    }
}
