using System.Runtime.Serialization;
using StarryEyes.Vanille.Serialization;

namespace StarryEyes.Breezy.DataModel
{
    [DataContract]
    public class TwitterEntity : IBinarySerializable
    {
        public TwitterEntity() { }

        public TwitterEntity(EntityType entityType,
            string display, string original, string media, int start, int end)
            : this(entityType, display, original, start, end)
        {
            this.MediaUrl = media;
        }

        public TwitterEntity(EntityType entityType,
            string display, string original, int start, int end)
        {
            this.EntityType = entityType;
            this.DisplayText = display;
            this.OriginalText = original;
            if (string.IsNullOrEmpty(original))
            {
                this.OriginalText = display;
            }
            this.StartIndex = start;
            this.EndIndex = end;
        }

        /// <summary>
        /// Internal ID
        /// </summary>
        public long InternalId { get; set; }

        /// <summary>
        /// Type of this entity.
        /// </summary>
        [DataMember]
        public EntityType EntityType { get; set; }

        /// <summary>
        /// String which represents displaying text. <para />
        /// </summary>
        [DataMember]
        public string DisplayText { get; set; }

        /// <summary>
        /// String that represents original information. <para />
        /// If this entity describes URL, this property may have original(unshortened) url. <para />
        /// If this entity describes User, this property may have numerical ID of user. <para />
        /// Otherwise, it has simply copy of Display string.
        /// </summary>
        [DataMember]
        public string OriginalText { get; set; }

        /// <summary>
        /// Url of media. used only for Media entity.
        /// </summary>
        [DataMember]
        public string MediaUrl { get; set; }

        /// <summary>
        /// Start index of this element
        /// </summary>
        [DataMember]
        public int StartIndex { get; set; }

        /// <summary>
        /// End index of this element
        /// </summary>
        [DataMember]
        public int EndIndex { get; set; }

        public void Serialize(System.IO.BinaryWriter writer)
        {
            writer.Write((int)EntityType);
            writer.Write(DisplayText ?? string.Empty);
            writer.Write(OriginalText ?? string.Empty);
            writer.Write(MediaUrl != null);
            if (MediaUrl != null)
                writer.Write(MediaUrl);
            writer.Write(StartIndex);
            writer.Write(EndIndex);
        }

        public void Deserialize(System.IO.BinaryReader reader)
        {
            EntityType = (DataModel.EntityType)reader.ReadInt32();
            DisplayText = reader.ReadString();
            OriginalText = reader.ReadString();
            if (reader.ReadBoolean())
                MediaUrl = reader.ReadString();
            StartIndex = reader.ReadInt32();
            EndIndex = reader.ReadInt32();
        }
    }

    public enum EntityType
    {
        Media,
        Urls,
        UserMentions,
        Hashtags
    }
}
