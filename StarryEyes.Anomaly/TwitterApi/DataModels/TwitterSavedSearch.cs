
using System;
using StarryEyes.Anomaly.Utils;

namespace StarryEyes.Anomaly.TwitterApi.DataModels
{
    public class TwitterSavedSearch
    {
        public TwitterSavedSearch() { }

        public TwitterSavedSearch(dynamic json)
        {
            Id = ((string)json.id_str).ParseLong();
            CreatedAt = ((string)json.created_at)
                .ParseDateTime(ParsingExtension.TwitterDateTimeFormat);
            Name = json.name;
            Query = json.query;
        }

        public long Id { get; set; }

        public DateTime CreatedAt { get; set; }

        public string Name { get; set; }

        public string Query { get; set; }

        // "position" attribute is contained in object which twitter returns,
        // but that always indicates null.
    }
}
