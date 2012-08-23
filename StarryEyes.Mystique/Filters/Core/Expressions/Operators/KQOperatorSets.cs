using System;
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
            return _ => RightValue.GetSetValue(_).Contains(LeftValue.GetNumericValue(_));
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
            return _ => LeftValue.GetSetValue(_).Contains(RightValue.GetElementValue(_));
        }
    }
}
