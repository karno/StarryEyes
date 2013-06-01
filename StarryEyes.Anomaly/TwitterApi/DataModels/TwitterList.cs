using System;
using System.Collections.Generic;
using StarryEyes.Anomaly.Utils;

namespace StarryEyes.Anomaly.TwitterApi.DataModels
{
    public class TwitterList
    {
        private const string TwitterListUriPrefix = "http://twitter.com";

        public TwitterList() { }
        public TwitterList(dynamic json)
        {
            Id = Int64.Parse(json.id_str);
            User = new TwitterUser(json.user);
            Name = json.name;
            FullName = json.full_name;
            Uri = new Uri(TwitterListUriPrefix + json.uri);
            Slug = json.slug;
            ListMode = String.Equals(json.mode, "public", StringComparison.InvariantCultureIgnoreCase)
                           ? ListMode.Public
                           : ListMode.Private;
            Description = json.description;
            MemberCount = json.member_count;
            SubscriberCount = json.subscriber_count;
            CreatedAt = ((string)json.created_at).ParseDateTime(ParsingExtension.TwitterDateTimeFormat);
        }

        /// <summary>
        /// ID of this list.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Created user
        /// </summary>
        public TwitterUser User { get; set; }

        /// <summary>
        /// Name of this list.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Full name of this list.
        /// </summary>
        public string FullName { get; set; }

        /// <summary>
        /// Uri for this list.
        /// </summary>
        public Uri Uri { get; set; }

        /// <summary>
        /// Slug of this list.
        /// </summary>
        public string Slug { get; set; }

        /// <summary>
        /// State of this list.
        /// </summary>
        public ListMode ListMode { get; set; }

        /// <summary>
        /// Description of this list.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Sum of members in this list.
        /// </summary>
        public long MemberCount { get; set; }

        /// <summary>
        /// Sum of subscribers this list.
        /// </summary>
        public long SubscriberCount { get; set; }

        /// <summary>
        /// Created timestamp of this list.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        public IEnumerable<long> MemberIds { get; set; }
    }

    public enum ListMode
    {
        Public,
        Private,
    }
}