using System;
using System.Collections.Generic;
using System.Linq;
using StarryEyes.SweetLady.DataModel;

namespace StarryEyes.Mystique.Filters.Expressions.Operators
{
    /// <summary>
    /// Contains as member
    /// </summary>
    public class FilterOperatorContains : FilterTwoValueOperator
    {
        protected override string OperatorString
        {
            get { return "->"; }
        }

        public override Func<TwitterStatus, bool> GetBooleanValueProvider()
        {
            var lsp = LeftValue.GetSetValueProvider();
            if (RightValue.SupportedTypes.Contains(FilterExpressionType.Numeric))
            {
                var rnp = RightValue.GetNumericValueProvider();
                return _ => lsp(_).Contains(rnp(_));
            }
            else
            {
                var rsp = RightValue.GetSetValueProvider();
                return _ =>
                {
                    var ls = lsp(_);
                    return rsp(_).Any(id => ls.Contains(id));
                };
            }
        }

        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get { yield return FilterExpressionType.Boolean; }
        }
    }

    /// <summary>
    /// Contained as member
    /// </summary>
    public class FilterOperatorContainedBy : FilterTwoValueOperator
    {
        protected override string OperatorString
        {
            get { return "<-"; }
        }

        public override Func<TwitterStatus, bool> GetBooleanValueProvider()
        {
            var rsp = RightValue.GetSetValueProvider();
            if (LeftValue.SupportedTypes.Contains(FilterExpressionType.Numeric))
            {
                var lnp = LeftValue.GetNumericValueProvider();
                return _ => rsp(_).Contains(lnp(_));
            }
            else
            {
                var lsp = LeftValue.GetSetValueProvider();
                return _ =>
                {
                    var rs = rsp(_);
                    return lsp(_).Any(id => rs.Contains(id));
                };
            }
        }

        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get { yield return FilterExpressionType.Boolean; }
        }
    }
}
