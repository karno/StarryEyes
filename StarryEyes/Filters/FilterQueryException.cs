using System;
using System.Collections.Generic;
using System.Linq;
using StarryEyes.Filters.Expressions;

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
            return base.ToString() + Environment.NewLine + " クエリ: " + innerQuery;
        }

        public static FilterQueryException CreateException(string msg, string query)
        {
            return new FilterQueryException(msg, query);
        }

        public static FilterQueryException CreateUnsupportedType(string filter, FilterExpressionType transformFailedType, string innerQuery)
        {
            var msg = string.Format("フィルタ {0} は型 {1} へ変換できません。", filter, transformFailedType.ToString());
            return CreateException(msg, innerQuery);
        }

        public static FilterQueryException CreateUnsupportedType(string filter, IEnumerable<FilterExpressionType> transformFailedTypes, string innerQuery)
        {
            var msg = string.Format("フィルタ {0} は {1} のいずれの型へも変換できません。", filter,
                                    String.Join(", ", transformFailedTypes.Select(f => f.ToString())));
            return CreateException(msg, innerQuery);
        }
    }
}
