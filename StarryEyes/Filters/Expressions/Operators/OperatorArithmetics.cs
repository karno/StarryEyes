using System;
using System.Collections.Generic;
using System.Linq;
using StarryEyes.Breezy.DataModel;

namespace StarryEyes.Filters.Expressions.Operators
{
    public sealed class FilterOperatorPlus : FilterTwoValueOperator
    {
        protected override string OperatorString
        {
            get { return "+"; }
        }

        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get
            {
                return new[] { FilterExpressionType.Numeric, FilterExpressionType.Set }
                    .Intersect(LeftValue.SupportedTypes)
                    .Intersect(RightValue.SupportedTypes);
            }
        }

        public override Func<TwitterStatus, ICollection<long>> GetSetValueProvider()
        {
            if (!FilterExpressionUtil.Assert(FilterExpressionType.Set, LeftValue.SupportedTypes, RightValue.SupportedTypes))
                throw new FilterQueryException("Each side of expression is must be convertable to long.", this.ToQuery());
            var lsp = LeftValue.GetSetValueProvider();
            var rsp = RightValue.GetSetValueProvider();
            return _ => lsp(_).Union(rsp(_)).ToList();
        }

        public override Func<TwitterStatus, long> GetNumericValueProvider()
        {
            if (!FilterExpressionUtil.Assert(FilterExpressionType.Numeric, LeftValue.SupportedTypes, RightValue.SupportedTypes))
                throw new FilterQueryException("Each side of expression is must be convertable to long.", this.ToQuery());
            var lnp = LeftValue.GetNumericValueProvider();
            var rnp = RightValue.GetNumericValueProvider();
            return _ => lnp(_) + rnp(_);
        }
    }

    public sealed class FilterOperatorMinus : FilterTwoValueOperator
    {
        protected override string OperatorString
        {
            get { return "-"; }
        }

        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get
            {
                return new[] { FilterExpressionType.Numeric, FilterExpressionType.Set }
                    .Intersect(LeftValue.SupportedTypes)
                    .Intersect(RightValue.SupportedTypes);
            }
        }

        public override Func<TwitterStatus, ICollection<long>> GetSetValueProvider()
        {
            if (!FilterExpressionUtil.Assert(FilterExpressionType.Set, LeftValue.SupportedTypes, RightValue.SupportedTypes))
                throw new FilterQueryException("Each side of expression is must be convertable to long.", this.ToQuery());
            var lsp = LeftValue.GetSetValueProvider();
            var rsp = RightValue.GetSetValueProvider();
            return _ => lsp(_).Except(rsp(_)).ToList();
        }

        public override Func<TwitterStatus, long> GetNumericValueProvider()
        {
            if (!FilterExpressionUtil.Assert(FilterExpressionType.Numeric, LeftValue.SupportedTypes, RightValue.SupportedTypes))
                throw new FilterQueryException("Each side of expression is must be convertable to long.", this.ToQuery());
            var lnp = LeftValue.GetNumericValueProvider();
            var rnp = RightValue.GetNumericValueProvider();
            return _ => lnp(_) - rnp(_);
        }
    }

    public sealed class FilterOperatorProduct : FilterTwoValueOperator
    {
        protected override string OperatorString
        {
            get { return "*"; }
        }

        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get
            {
                return new[] { FilterExpressionType.Numeric, FilterExpressionType.Set }
                    .Intersect(LeftValue.SupportedTypes)
                    .Intersect(RightValue.SupportedTypes);
            }
        }

        public override Func<TwitterStatus, ICollection<long>> GetSetValueProvider()
        {
            if (!FilterExpressionUtil.Assert(FilterExpressionType.Set, LeftValue.SupportedTypes, RightValue.SupportedTypes))
                throw new FilterQueryException("Each side of expression is must be convertable to long.", this.ToQuery());
            var lsp = LeftValue.GetSetValueProvider();
            var rsp = RightValue.GetSetValueProvider();
            return _ => lsp(_).Intersect(rsp(_)).ToList();
        }

        public override Func<TwitterStatus, long> GetNumericValueProvider()
        {
            if (!FilterExpressionUtil.Assert(FilterExpressionType.Numeric, LeftValue.SupportedTypes, RightValue.SupportedTypes))
                throw new FilterQueryException("Each side of expression is must be convertable to long.", this.ToQuery());
            var lnp = LeftValue.GetNumericValueProvider();
            var rnp = RightValue.GetNumericValueProvider();
            return _ => lnp(_) * rnp(_);
        }
    }

    public sealed class FilterOperatorDivide : FilterTwoValueOperator
    {
        protected override string OperatorString
        {
            get { return "/"; }
        }

        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get
            {
                return new[] { FilterExpressionType.Numeric, FilterExpressionType.Set }
                    .Intersect(LeftValue.SupportedTypes)
                    .Intersect(RightValue.SupportedTypes);
            }
        }

        public override Func<TwitterStatus, ICollection<long>> GetSetValueProvider()
        {
            if (!FilterExpressionUtil.Assert(FilterExpressionType.Set, LeftValue.SupportedTypes, RightValue.SupportedTypes))
                throw new FilterQueryException("Each side of expression is must be convertable to long.", this.ToQuery());
            var lsp = LeftValue.GetSetValueProvider();
            var rsp = RightValue.GetSetValueProvider();
            return _ => lsp(_).Intersect(rsp(_)).ToList();
        }

        public override Func<TwitterStatus, long> GetNumericValueProvider()
        {
            if (!FilterExpressionUtil.Assert(FilterExpressionType.Numeric, LeftValue.SupportedTypes, RightValue.SupportedTypes))
                throw new FilterQueryException("Each side of expression is must be convertable to long.", this.ToQuery());
            var lnp = LeftValue.GetNumericValueProvider();
            var rnp = RightValue.GetNumericValueProvider();
            return _ =>
                {
                    var divider = rnp(_);
                    if (divider == 0)
                        return 0;
                    else
                        return lnp(_) / divider;
                };
        }
    }

}
