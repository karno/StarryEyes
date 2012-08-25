using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using StarryEyes.SweetLady.DataModel;

namespace StarryEyes.Mystique.Filters.Expressions.Operators
{
    public sealed class OperatorConcatenateAnd : FilterTwoValueOperator
    {
        protected override string OperatorString
        {
            get { return "&&"; }
        }

        public override Func<TwitterStatus, bool> GetBooleanValueProvider()
        {
            if (!FilterExpressionUtil.Assert(FilterExpressionType.Boolean, LeftValue.SupportedTypes, RightValue.SupportedTypes))
                throw new FilterQueryException("Each filters must be convertable as boolean.", this.ToQuery());
            var lbp = LeftValue.GetBooleanValueProvider();
            var rbp = RightValue.GetBooleanValueProvider();
            return _ => lbp(_) && rbp(_);
        }

        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get { yield return FilterExpressionType.Boolean; }
        }
    }

    public sealed class OperatorConcatenateOr : FilterTwoValueOperator
    {
        protected override string OperatorString
        {
            get { return "||"; }
        }

        public override Func<TwitterStatus, bool> GetBooleanValueProvider()
        {
            if (!FilterExpressionUtil.Assert(FilterExpressionType.Boolean, LeftValue.SupportedTypes, RightValue.SupportedTypes))
                throw new FilterQueryException("Each filters must be convertable as boolean.", this.ToQuery());
            var lbp = LeftValue.GetBooleanValueProvider();
            var rbp = RightValue.GetBooleanValueProvider();
            return _ => lbp(_) || rbp(_);
        }

        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get { yield return FilterExpressionType.Boolean; }
        }
    }
}
