using System.Collections.Generic;
using StarryEyes.SweetLady.DataModel;

namespace StarryEyes.Mystique.Filters.Core.Expressions.Values
{
    public abstract class ValueBase
    {
        public abstract KQExpressionType[] TransformableTypes { get; }

        public virtual bool GetBooleanValue(TwitterStatus status)
        {
            throw new KrileQueryException("Unsupported transforms to boolean: " + ToQuery());
        }

        public virtual long GetNumericValue(TwitterStatus status)
        {
            throw new KrileQueryException("Unsupported transforms to numeric: " + ToQuery());
        }

        public virtual string GetStringValue(TwitterStatus status)
        {
            throw new KrileQueryException("Unsupported transforms to string: " + ToQuery());
        }

        public virtual ICollection<long> GetSetValue(TwitterStatus status)
        {
            throw new KrileQueryException("Unsupported transforms to set: " + ToQuery());
        }
        
        public abstract string ToQuery();
    }
}
