using StarryEyes.Filters.Expressions.Operators;

namespace StarryEyes.Filters.Expressions.Values
{
    public abstract class ValueBase : FilterOperatorBase
    {
        protected override string OperatorString
        {
            get { return this.ToQuery(); }
        }
    }
}
