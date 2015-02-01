using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace StarryEyes.Anomaly.TwitterApi.Rest.Parameters
{
    public sealed class StatusParameter : ParameterBase
    {
        private string _status;

        [NotNull]
        public string Status
        {
            get { return _status; }
            set
            {
                if (value == null) throw new ArgumentNullException("value");
                _status = value;
            }
        }

        public long? InReplyToStatusId { get; set; }

        public bool? PossiblySensitive { get; set; }

        public Tuple<double, double> GeoLatLong { get; set; }

        public string PlaceId { get; set; }

        public bool? DisplayCoordinates { get; set; }

        public long[] MediaIds { get; set; }

        public StatusParameter([NotNull] string status, long? inReplyToStatusId = null,
            bool? possiblySensitive = null, [CanBeNull] Tuple<double, double> geoLatLong = null,
            [CanBeNull] string placeId = null, bool? displayCoordinates = null,
            [CanBeNull] long[] mediaIds = null)
        {
            if (status == null) throw new ArgumentNullException("status");
            _status = status;
            InReplyToStatusId = inReplyToStatusId;
            PossiblySensitive = possiblySensitive;
            GeoLatLong = geoLatLong;
            PlaceId = placeId;
            DisplayCoordinates = displayCoordinates;
            MediaIds = mediaIds;
        }

        public override void SetDictionary(Dictionary<string, object> target)
        {
            var mediaIdStr = MediaIds != null
                ? MediaIds.Select(s => s.ToString()).JoinString(",")
                : null;
            target["status"] = _status;
            target["in_reply_to_status_id"] = InReplyToStatusId;
            target["possibly_sensitive"] = PossiblySensitive;
            target["lat"] = GeoLatLong != null ? GeoLatLong.Item1 : (double?)null;
            target["long"] = GeoLatLong != null ? GeoLatLong.Item2 : (double?)null;
            target["place_id"] = PlaceId;
            target["display_coordinates"] = DisplayCoordinates;
            target["media_ids"] = mediaIdStr;
        }
    }
}
