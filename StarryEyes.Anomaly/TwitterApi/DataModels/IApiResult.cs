
using System;

namespace StarryEyes.Anomaly.TwitterApi.DataModels
{
    public interface IApiResult<out T>
    {
        RateLimitDescription RateLimit { get; }

        T Result { get; }
    }

    public struct RateLimitDescription
    {
        public RateLimitDescription(long limit, long remain, DateTime reset)
        {
            _limit = limit;
            _remain = remain;
            _reset = reset;
        }

        private readonly long _limit;

        private readonly long _remain;

        private readonly DateTime _reset;

        public long Limit
        {
            get { return _limit; }
        }

        public long Remain
        {
            get { return _remain; }
        }

        public DateTime Reset
        {
            get { return _reset; }
        }
    }
}
