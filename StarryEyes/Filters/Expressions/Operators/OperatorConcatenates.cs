using System;
using System.Collections.Generic;
using Cadena.Data;

namespace StarryEyes.Filters.Expressions.Operators
{
    public sealed class FilterOperatorAnd : FilterTwoValueOperator
    {
        protected override string OperatorString => "&";

        public override Func<TwitterStatus, bool> GetBooleanValueProvider()
        {
            var lbp = LeftValue.GetBooleanValueProvider();
            var rbp = RightValue.GetBooleanValueProvider();
            return _ => lbp(_) && rbp(_);
        }

        public override string GetBooleanSqlQuery()
        {
            return LeftValue.GetBooleanSqlQuery() + " AND " + RightValue.GetBooleanSqlQuery();
        }

        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get { yield return FilterExpressionType.Boolean; }
        }
    }

    public sealed class FilterOperatorOr : FilterTwoValueOperator
    {
        protected override string OperatorString => "|";

        public override Func<TwitterStatus, bool> GetBooleanValueProvider()
        {
            var lbp = LeftValue.GetBooleanValueProvider();
            var rbp = RightValue.GetBooleanValueProvider();
            return _ => lbp(_) || rbp(_);
        }

        public override string GetBooleanSqlQuery()
        {
            return LeftValue.GetBooleanSqlQuery() + " OR " + RightValue.GetBooleanSqlQuery();
        }

        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get { yield return FilterExpressionType.Boolean; }
        }
    }
}