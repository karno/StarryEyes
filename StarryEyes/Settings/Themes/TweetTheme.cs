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
    public class TweetTheme : IResourceConfigurator, ICloneable
    {
        #region Serialization properties

        [DataMember(Order = 2)]
        private string ForegroundKeyColor
        {
            get => KeyText.ToColorString();
            set => KeyText = value.ToColor();
        }

        [DataMember(Order = 3)]
        private string ForegroundSubColor
        {
            get => SubText.ToColorString();
            set => SubText = value.ToColor();
        }

        [DataMember(Order = 4)]
        private string ForegroundLinkColor
        {
            get => HyperlinkText.ToColorString();
            set => HyperlinkText = value.ToColor();
        }

        [DataMember(Order = 5)]
        private string FavoriteCounterColor
        {
            get => FavoriteCounter.ToColorString();
            set => FavoriteCounter = value.ToColor();
        }

        [DataMember(Order = 6)]
        private string RetweetCounterColor
        {
            get => RetweetCounter.ToColorString();
            set => RetweetCounter = value.ToColor();
        }

        [DataMember(Order = 7)]
        private string RetweetMarkerColor
        {
            get => RetweetMarker.ToColorString();
            set => RetweetMarker = value.ToColor();
        }

        #endregion Serialization properties

        /// <summary>
        /// Colors for default tweet
        /// </summary>
        [DataMember(Order = 0)]
        public ThemeColors Default { get; set; }

        /// <summary>
        /// Colors for highlighted tweet
        /// </summary>
        [DataMember(Order = 1)]
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
        /// Link text color
        /// </summary>
        [IgnoreDataMember]
        public Color HyperlinkText { get; set; }

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
        [DataMember(Order = 8)]
        public ControlColors FavoriteAndRetweetButton { get; set; }

        /// <summary>
        /// Colors for favorite button
        /// </summary>
        [DataMember(Order = 9)]
        public ControlColors FavoriteButton { get; set; }

        /// <summary>
        /// Colors for favorited favorite button
        /// </summary>
        [DataMember(Order = 10)]
        public ControlColors ColoredFavoriteButton { get; set; }

        /// <summary>
        /// Colors for retweet button
        /// </summary>
        [DataMember(Order = 11)]
        public ControlColors RetweetButton { get; set; }

        /// <summary>
        /// Colors for retweeted retweet button
        /// </summary>
        [DataMember(Order = 12)]
        public ControlColors ColoredRetweetButton { get; set; }

        /// <summary>
        /// Colors for mention button
        /// </summary>
        [DataMember(Order = 13)]
        public ControlColors MentionButton { get; set; }

        /// <summary>
        /// Colors for delete button
        /// </summary>
        [DataMember(Order = 14)]
        public ControlColors DeleteButton { get; set; }

        public void ConfigureResourceDictionary(ResourceDictionary dictionary, string prefix)
        {
            Default.ConfigureResourceDictionary(dictionary, prefix);
            Highlight.ConfigureResourceDictionary(dictionary, prefix + "Highlight");
            KeyText.ConfigureResourceDictionary(dictionary, prefix + "KeyText");
            SubText.ConfigureResourceDictionary(dictionary, prefix + "SubText");
            HyperlinkText.ConfigureResourceDictionary(dictionary, prefix + "HyperlinkText");
            FavoriteCounter.ConfigureResourceDictionary(dictionary, prefix + "FavoriteCounter");
            RetweetCounter.ConfigureResourceDictionary(dictionary, prefix + "RetweetCounter");
            RetweetMarker.ConfigureResourceDictionary(dictionary, prefix + "RetweetMarker");
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
                Default = Default.Clone(),
                Highlight = Highlight.Clone(),
                KeyText = KeyText,
                SubText = SubText,
                HyperlinkText = HyperlinkText,
                FavoriteCounter = FavoriteCounter,
                RetweetCounter = RetweetCounter,
                RetweetMarker = RetweetMarker,
                FavoriteAndRetweetButton = FavoriteAndRetweetButton.Clone(),
                FavoriteButton = FavoriteButton.Clone(),
                ColoredFavoriteButton = ColoredFavoriteButton.Clone(),
                RetweetButton = RetweetButton.Clone(),
                ColoredRetweetButton = ColoredRetweetButton.Clone(),
                MentionButton = MentionButton.Clone(),
                DeleteButton = DeleteButton.Clone()
            };
        }

        object ICloneable.Clone()
        {
            return Clone();
        }
    }
}