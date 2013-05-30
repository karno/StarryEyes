namespace StarryEyes.Casket.DataModels
{
    public class DbEntity
    {
        /// <summary>
        /// Item Key
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Binding parent Id, status id or user id
        /// </summary>
        public long ParentId { get; set; }

        /// <summary>
        /// Entity kind
        /// </summary>
        public DbEntityKind Kind { get; set; }

        /// <summary>
        /// Beginning of entity attachment
        /// </summary>
        public int IndiceStart { get; set; }

        /// <summary>
        /// Ending of entity attachment
        /// </summary>
        public int IndiceEnd { get; set; }

        /// <summary>
        /// Entity text
        /// </summary>
        public string DisplayText { get; set; }

        /// <summary>
        /// Url where should user navigate
        /// </summary>
        public string NavigationUrl { get; set; }

        /// <summary>
        /// Url where load remote resources, etc.
        /// </summary>
        public string RepresentUrl { get; set; }
    }

    public enum DbEntityKind : byte
    {
        Hashtags,
        Media,
        Urls,
        UserMentions
    }
}
