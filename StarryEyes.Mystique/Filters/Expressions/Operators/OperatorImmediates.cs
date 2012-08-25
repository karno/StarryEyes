using System;
using System.Collections.Generic;
using StarryEyes.SweetLady.DataModel;

namespace StarryEyes.Mystique.Filters.Expressions.Operators
{
    public class FilterNegate : FilterSingleValueOperator
    {
        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get { yield return FilterExpressionType.Boolean; }
        }

        public override Func<TwitterStatus, bool> GetBooleanValueProvider()
        {
            if (!FilterExpressionUtil.Assert(FilterExpressionType.Boolean, Value.SupportedTypes))
                throw new FilterQueryException("Negate operator must be have boolean value.", ToQuery());
            return _ => !Value.GetBooleanValueProvider()(_);
        }

        public override string ToQuery()
        {
            return "!" + Value.ToQuery();
        }
    }
}
