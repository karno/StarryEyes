using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace StarryEyes.Anomaly.TwitterApi.Rest.Parameter
{
    public sealed class SearchParameter : ParameterBase
    {
        private string _query;

        public SearchResultType ResultType { get; private set; }

        [CanBeNull]
        public string GeoCode { get; private set; }

        [CanBeNull]
        public string Lang { get; private set; }

        [CanBeNull]
        public string Locale { get; private set; }

        public int? Count { get; private set; }

        public DateTime? UntilDate { get; private set; }

        public long? SinceId { get; private set; }

        public long? MaxId { get; private set; }

        public SearchParameter([NotNull] string query, SearchResultType resultType = SearchResultType.Mixed,
           [CanBeNull] string geoCode = null, [CanBeNull] string lang = null, [CanBeNull] string locale = null,
           int? count = null, DateTime? untilDate = null, long? sinceId = null, long? maxId = null)
        {
            _query = query;
            ResultType = resultType;
            GeoCode = geoCode;
            Lang = lang;
            Locale = locale;
            Count = count;
            UntilDate = untilDate;
            SinceId = sinceId;
            MaxId = maxId;
            if (query == null) throw new ArgumentNullException("query");
        }

        [NotNull]
        public string Query
        {
            get { return _query; }
            set
            {
                if (value == null) throw new ArgumentNullException("value");
                _query = value;
            }
        }

        public override void SetDictionary(Dictionary<string, object> target)
        {
            target["q"] = _query;
            target["geocode"] = GeoCode;
            target["lang"] = Lang;
            target["locale"] = Locale;
            target["result_type"] = ResultType.ToString().ToLower();
            target["count"] = Count;
            target["until"] = UntilDate != null ? UntilDate.Value.ToString("yyyy-MM-dd") : null;
            target["since_id"] = SinceId;
            target["max_id"] = MaxId;
        }
    }
}
