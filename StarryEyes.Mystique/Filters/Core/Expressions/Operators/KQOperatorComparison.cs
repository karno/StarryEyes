using System;
using StarryEyes.SweetLady.DataModel;

namespace StarryEyes.Mystique.Filters.Core.Expressions.Operators
{
    public class KQOperatorLesserThan : KQOperatorBase
    {
        public override string ToQuery()
        {
            return LeftValue.ToQuery() + " < " + RightValue.ToQuery();
        }

        public override Func<TwitterStatus, bool> GetEvaluator()
        {
            // assert type
            KQExpressionUtil.CheckDecide(
                LeftValue.TransformableTypes,
                RightValue.TransformableTypes,
                new[] { KQExpressionType.Numeric });
            return _ => LeftValue.GetNumericValue(_) < RightValue.GetNumericValue(_);

        }
    }

    public class KQOperatorLesserThanOrEqual : KQOperatorBase
    {
        public override string ToQuery()
        {
            return LeftValue.ToQuery() + " <= " + RightValue.ToQuery();
        }

        public override Func<TwitterStatus, bool> GetEvaluator()
        {
            // assert type
            KQExpressionUtil.CheckDecide(
                LeftValue.TransformableTypes,
                RightValue.TransformableTypes,
                new[] { KQExpressionType.Numeric });
            return _ => LeftValue.GetNumericValue(_) <= RightValue.GetNumericValue(_);
        }
    }

    public class KQOperatorGreaterThan : KQOperatorBase
    {
        public override string ToQuery()
        {
            return LeftValue.ToQuery() + " > " + RightValue.ToQuery();
        }

        public override Func<TwitterStatus, bool> GetEvaluator()
        {
            // assert type
            KQExpressionUtil.CheckDecide(
                LeftValue.TransformableTypes,
                RightValue.TransformableTypes,
                new[] { KQExpressionType.Numeric });
            return _ => LeftValue.GetNumericValue(_) > RightValue.GetNumericValue(_);
        }
    }

    public class KQOperatorGreaterThanOrEqual : KQOperatorBase
    {
        public override string ToQuery()
        {
            return LeftValue.ToQuery() + " > " + RightValue.ToQuery();
        }

        public override Func<TwitterStatus, bool> GetEvaluator()
        {
            // assert type
            KQExpressionUtil.CheckDecide(
                LeftValue.TransformableTypes,
                RightValue.TransformableTypes,
                new[] { KQExpressionType.Numeric });
            return _ => LeftValue.GetNumericValue(_) >= RightValue.GetNumericValue(_);
        }
    }
}
