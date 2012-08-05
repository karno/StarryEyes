using System;
using System.Runtime.Serialization;

namespace StarryEyes.SweetLady.DataModel
{
    [DataContract]
    public class TwitterList
    {
        [DataMember]
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// ID of this list.
        /// </summary>
        [DataMember]
        public long Id { get; set; }

        /// <summary>
        /// Created user
        /// </summary>
        [DataMember]
        public TwitterUser User { get; set; }

        /// <summary>
        /// Name of this list.
        /// </summary>
        [DataMember]
        public string Name { get; set; }

        /// <summary>
        /// Full name of this list.
        /// </summary>
        [DataMember]
        public string FullName { get; set; }

        /// <summary>
        /// Uri for this list.
        /// </summary>
        [DataMember]
        public Uri Uri { get; set; }

        /// <summary>
        /// Slug of this list.
        /// </summary>
        [DataMember]
        public string Slug { get; set; }

        /// <summary>
        /// State of this list.
        /// </summary>
        [DataMember]
        public ListMode ListMode { get; set; }

        /// <summary>
        /// Description of this list.
        /// </summary>
        [DataMember]
        public string Description { get; set; }

        /// <summary>
        /// Sum of members in this list.
        /// </summary>
        [DataMember]
        public long MemberCount { get; set; }

        /// <summary>
        /// Sum of subscribers this list.
        /// </summary>
        [DataMember]
        public long SubscriberCount { get; set; }

        /// <summary>
        /// Created timestamp of this list.
        /// </summary>
        [DataMember]
        public DateTime CreatedAt { get; set; }
    }

    public enum ListMode
    {
        Public,
        Private,
    }
}