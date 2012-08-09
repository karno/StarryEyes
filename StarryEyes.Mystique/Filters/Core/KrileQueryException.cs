using System;

namespace StarryEyes.Mystique.Filters.Core
{
    [Serializable]
    public class KrileQueryException : Exception
    {
        public KrileQueryException() { }
        public KrileQueryException(string message) : base(message) { }
        public KrileQueryException(string message, Exception inner) : base(message, inner) { }
        protected KrileQueryException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}
