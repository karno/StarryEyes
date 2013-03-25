using System.Linq;
using StarryEyes.Breezy.DataModel;
using StarryEyes.Breezy.Util;

namespace StarryEyes.Breezy.Api.Parsing.JsonFormats
{
    public class DirectMessageWrappedJson
    {
        public DirectMessageJson direct_message { get; set; }

        public TwitterStatus Spawn()
        {
            return direct_message.Spawn();
        }
    }
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
                Text = XmlParser.ResolveEntity(text),
                CreatedAt = created_at.ParseDateTime(XmlParser.TwitterDateTimeFormat),
                Recipient = recipient.Spawn(),
                Entities = entities != null ? entities.Spawn().ToArray() : new TwitterEntity[0]
            };
        }
    }
}
