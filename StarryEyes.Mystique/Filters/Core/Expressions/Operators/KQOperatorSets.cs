using System;
using System.Linq;
using StarryEyes.SweetLady.DataModel;

namespace StarryEyes.Mystique.Filters.Core.Expressions.Operators
{
    /// <summary>
    /// Contains as member
    /// </summary>
    public class KQOperatorContains : KQOperatorBase
    {
        public override string ToQuery()
        {
            return LeftValue.ToQuery() + " <- " + RightValue.ToQuery();
        }

        public override Func<TwitterStatus, bool> GetEvaluator()
        {
            if (LeftValue.TransformableTypes.Contains(KQExpressionType.Numeric))
            {
                return _ => RightValue.GetSetValue(_).Contains(LeftValue.GetNumericValue(_));
            }
            else
            {
                return _ => LeftValue.GetSetValue(_).Any(id => RightValue.GetSetValue(_).Contains(id));
            }
        }
    }

    /// <summary>
    /// Contained as member
    /// </summary>
    public class KQOperatorContainedBy : KQOperatorBase
    {
        public override string ToQuery()
        {
            return LeftValue.ToQuery() + " -> " + RightValue.ToQuery();
        }

        public override Func<TwitterStatus, bool> GetEvaluator()
        {
            if (RightValue.TransformableTypes.Contains(KQExpressionType.Numeric))
            {
                return _ => LeftValue.GetSetValue(_).Contains(RightValue.GetNumericValue(_));
            }
            else
            {
                return _ => RightValue.GetSetValue(_).Any(id => LeftValue.GetSetValue(_).Contains(id));
            }
        }
    }
}
