using System;
using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Media;

namespace StarryEyes.Settings.Themes
{
    /// <summary>
    /// Describes tweet theme
    /// </summary>
    [DataContract]
    public struct TweetTheme : IResourceConfigurator, ICloneable
    {
        #region Serialization properties

        [DataMember]
        private string ForegroundKeyColor
        {
            get { return this.KeyText.ToColorString(); }
            set { this.KeyText = value.ToColor(); }
        }

        [DataMember]
        private string ForegroundSubColor
        {
            get { return this.SubText.ToColorString(); }
            set { this.SubText = value.ToColor(); }
        }

        [DataMember]
        private string FavoriteCounterColor
        {
            get { return FavoriteCounter.ToColorString(); }
            set { FavoriteCounter = value.ToColor(); }
        }

        [DataMember]
        private string RetweetCounterColor
        {
            get { return RetweetCounter.ToColorString(); }
            set { RetweetCounter = value.ToColor(); }
        }

        [DataMember]
        private string RetweetMarkerColor
        {
            get { return RetweetMarker.ToColorString(); }
            set { RetweetMarker = value.ToColor(); }
        }

        #endregion

        /// <summary>
        /// Colors for default tweet
        /// </summary>
        [DataMember]
        public ThemeColors Default { get; set; }

        /// <summary>
        /// Colors for highlighted tweet
        /// </summary>
        [DataMember]
        public ThemeColors Highlight { get; set; }

        /// <summary>
        /// Key text color (e.g. screen name)
        /// </summary>
        [IgnoreDataMember]
        public Color KeyText { get; set; }

        /// <summary>
        /// Sub text color (e.g. time, client, etc...)
        /// </summary>
        [IgnoreDataMember]
        public Color SubText { get; set; }

        /// <summary>
        /// Foreground color for favorite counter
        /// </summary>
        [IgnoreDataMember]
        public Color FavoriteCounter { get; set; }

        /// <summary>
        /// Foreground color for retweet counter
        /// </summary>
        [IgnoreDataMember]
        public Color RetweetCounter { get; set; }

        /// <summary>
        /// Foreground color for retweeted mark
        /// </summary>
        [IgnoreDataMember]
        public Color RetweetMarker { get; set; }

        /// <summary>
        /// Colors for favorite and retweet button
        /// </summary>
        [DataMember]
        public ControlColors FavoriteAndRetweetButton { get; set; }

        /// <summary>
        /// Colors for favorite button
        /// </summary>
        [DataMember]
        public ControlColors FavoriteButton { get; set; }

        /// <summary>
        /// Colors for favorited favorite button
        /// </summary>
        [DataMember]
        public ControlColors ColoredFavoriteButton { get; set; }

        /// <summary>
        /// Colors for retweet button
        /// </summary>
        [DataMember]
        public ControlColors RetweetButton { get; set; }

        /// <summary>
        /// Colors for retweeted retweet button
        /// </summary>
        [DataMember]
        public ControlColors ColoredRetweetButton { get; set; }

        /// <summary>
        /// Colors for mention button
        /// </summary>
        [DataMember]
        public ControlColors MentionButton { get; set; }

        /// <summary>
        /// Colors for delete button
        /// </summary>
        [DataMember]
        public ControlColors DeleteButton { get; set; }

        public void ConfigureResourceDictionary(ResourceDictionary dictionary, string prefix)
        {
            Default.ConfigureResourceDictionary(dictionary, prefix);
            Highlight.ConfigureResourceDictionary(dictionary, prefix + "Highlight");
            KeyText.ConfigureResourceDictionary(dictionary, prefix + "KeyText");
            SubText.ConfigureResourceDictionary(dictionary, prefix + "SubText");
            FavoriteAndRetweetButton.ConfigureResourceDictionary(dictionary, prefix + "FavAndRtButton");
            FavoriteButton.ConfigureResourceDictionary(dictionary, prefix + "FavoriteButton");
            ColoredFavoriteButton.ConfigureResourceDictionary(dictionary, prefix + "ColoredFavoriteButton");
            RetweetButton.ConfigureResourceDictionary(dictionary, prefix + "RetweetButton");
            ColoredRetweetButton.ConfigureResourceDictionary(dictionary, prefix + "ColoredRetweetButton");
            MentionButton.ConfigureResourceDictionary(dictionary, prefix + "MentionButton");
            DeleteButton.ConfigureResourceDictionary(dictionary, prefix + "DeleteButton");
        }

        public TweetTheme Clone()
        {
            return new TweetTheme
            {
                Default = Default,
                Highlight = Highlight,
                KeyText = KeyText,
                SubText = SubText,
                FavoriteAndRetweetButton = FavoriteAndRetweetButton,
                FavoriteButton = FavoriteButton,
                ColoredFavoriteButton = ColoredFavoriteButton,
                RetweetButton = RetweetButton,
                ColoredRetweetButton = ColoredRetweetButton,
                MentionButton = MentionButton,
                DeleteButton = DeleteButton
            };
        }

        object ICloneable.Clone()
        {
            return Clone();
        }
    }
}
