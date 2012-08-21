using System.Collections.Generic;
using StarryEyes.SweetLady.DataModel;

namespace StarryEyes.Mystique.Filters.Core.Expressions.Values
{
    public abstract class ValueBase
    {
        public abstract KQExpressionType[] TransformableTypes { get; }

        public virtual bool GetBooleanValue(TwitterStatus status)
        {
            throw new KrileQueryException("Unsupported transforms to boolean.");
        }

        public virtual long GetNumericValue(TwitterStatus status)
        {
            throw new KrileQueryException("Unsupported transforms to numeric.");
        }

        public virtual string GetStringValue(TwitterStatus status)
        {
            throw new KrileQueryException("Unsupported transforms to string.");
        }

        public virtual long GetElementValue(TwitterStatus status)
        {
            throw new KrileQueryException("Unsupported transforms to element.");
        }

        public virtual IEnumerable<long> GetSetValue(TwitterStatus status)
        {
            throw new KrileQueryException("Unsupported transforms to set.");
        }
        
        public abstract string ToQuery();
    }
}
