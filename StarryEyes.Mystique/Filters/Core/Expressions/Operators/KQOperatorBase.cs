using System;
using StarryEyes.Mystique.Filters.Core.Expressions.Values;
using StarryEyes.SweetLady.DataModel;

namespace StarryEyes.Mystique.Filters.Core.Expressions.Operators
{
    public abstract class KQOperatorBase : KQExpressionBase
    {
        public ValueBase LeftValue { get; set; }

        public ValueBase RightValue { get; set; }
    }
}
