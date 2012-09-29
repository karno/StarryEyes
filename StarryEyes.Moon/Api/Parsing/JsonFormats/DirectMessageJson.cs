using System.Linq;
using StarryEyes.Moon.DataModel;
using StarryEyes.Moon.Util;

namespace StarryEyes.Moon.Api.Parsing.JsonFormats
{
    public class DirectMessageJson
    {
        public string id_str { get; set; }

        public UserJson sender { get; set; }

        public string text { get; set; }

        public string created_at { get; set; }

        public UserJson recipient { get; set; }

        public EntityJson entities { get; set; }

        /// <summary>
        /// Create actual TwitterStatus.
        /// </summary>
        public TwitterStatus Spawn()
        {
            return new TwitterStatus()
            {
                StatusType = StatusType.DirectMessage,
                Id = id_str.ParseLong(),
                User = sender.Spawn(),
                Text = text,
                CreatedAt = created_at.ParseDateTime(XmlParser.TwitterDateTimeFormat),
                Recipient = recipient.Spawn(),
                Entities = entities != null ? entities.Spawn().ToArray() : new TwitterEntity[0]
            };
        }
    }
}
