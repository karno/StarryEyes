using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.Utils;

namespace StarryEyes.Anomaly.TwitterApi.Internals
{
    internal sealed class ApiResult<T> : IApiResult<T>
    {
        private readonly RateLimitDescription _limit;

        private readonly T _result;

        public ApiResult(T result, long limit, long remain, long reset)
            : this(result, limit, remain, UnixEpoch.GetDateTimeByUnixEpoch(reset))
        {
        }

        public ApiResult(T result, long limit, long remain, DateTime reset)
        {
            _limit = new RateLimitDescription(limit, remain, reset);
            _result = result;
        }

        public RateLimitDescription RateLimit
        {
            get { return _limit; }
        }

        public T Result
        {
            get { return _result; }
        }
    }

    internal static class ApiResult
    {
        public static string HeaderRateLimitLimit = "X-Rate-Limit-Limit";

        public static string HeaderRateLimitRemaining = "X-Rate-Limit-Remaining";

        public static string HeaderRateLimitReset = "X-Rate-Limit-Reset";

        public static ApiResult<T> Create<T>(T item, HttpResponseMessage message)
        {
            var limit = GetFirstHeaderOrNull(message, HeaderRateLimitLimit).ParseLong();
            var remain = GetFirstHeaderOrNull(message, HeaderRateLimitRemaining).ParseLong();
            var reset = GetFirstHeaderOrNull(message, HeaderRateLimitReset).ParseLong();
            return new ApiResult<T>(item, limit, remain, reset);
        }

        private static string GetFirstHeaderOrNull(HttpResponseMessage message, string key)
        {
            IEnumerable<string> values;
            return message.Headers.TryGetValues(key, out values) ? values.FirstOrDefault() : null;
        }
    }
}
