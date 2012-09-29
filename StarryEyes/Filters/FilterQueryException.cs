using System;

namespace StarryEyes.Filters
{
    [Serializable]
    public class FilterQueryException : Exception
    {
        private string innerQuery;
        public string InnerQuery
        {
            get { return innerQuery; }
            set { innerQuery = value; }
        }

        public FilterQueryException(string innerQuery)
        {
            this.innerQuery = innerQuery;
        }
        public FilterQueryException(string message, string innerQuery)
            : base(message)
        {
            this.innerQuery = innerQuery;
        }
        public FilterQueryException(string message, string innerQuery, Exception inner)
            : base(message, inner)
        {
            this.innerQuery = innerQuery;
        }
        protected FilterQueryException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }

        public override string ToString()
        {
            return base.ToString() + Environment.NewLine + " Query: " + innerQuery;
        }
    }
}
