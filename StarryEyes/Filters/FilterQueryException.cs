using System;
using System.Collections.Generic;
using System.Linq;
using StarryEyes.Filters.Expressions;
using StarryEyes.Globalization;
using StarryEyes.Globalization.Filters;

namespace StarryEyes.Filters
{
    [Serializable]
    public class FilterQueryException : Exception
    {
        private string _innerQuery;

        public string InnerQuery
        {
            get => _innerQuery;
            set => _innerQuery = value;
        }

        public FilterQueryException(string innerQuery)
        {
            _innerQuery = innerQuery;
        }

        public FilterQueryException(string message, string innerQuery)
            : base(message)
        {
            _innerQuery = innerQuery;
        }

        public FilterQueryException(string message, string innerQuery, Exception inner)
            : base(message, inner)
        {
            _innerQuery = innerQuery;
        }

        protected FilterQueryException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
        }

        public override string ToString()
        {
            return base.ToString() + Environment.NewLine + " " + QueryCompilerResources.FilterQueryExceptionQuery +
                   " " + _innerQuery;
        }

        public static FilterQueryException CreateException(string msg, string query)
        {
            return new FilterQueryException(msg, query);
        }

        public static FilterQueryException CreateUnsupportedType(string filter,
            FilterExpressionType transformFailedType, string innerQuery)
        {
            var msg = QueryCompilerResources
                .FilterQueryExceptionUnsupportedTypeFormat
                .SafeFormat(filter, transformFailedType.ToString());
            return CreateException(msg, innerQuery);
        }

        public static FilterQueryException CreateUnsupportedType(string filter,
            IEnumerable<FilterExpressionType> transformFailedTypes, string innerQuery)
        {
            var msg = QueryCompilerResources
                .FilterQueryExceptionUnsupportedTypeManyFormat
                .SafeFormat(filter, String.Join(", ", transformFailedTypes.Select(f => f.ToString())));
            return CreateException(msg, innerQuery);
        }
    }
}