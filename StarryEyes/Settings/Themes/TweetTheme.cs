using System.Runtime.Serialization;
using System.Windows.Media;

namespace StarryEyes.Settings.Themes
{
    [DataContract]
    public struct TweetTheme
    {
        #region Serialization properties

        [DataMember]
        private string ForegroundKeyColor
        {
            get { return ForegroundKey.ToColorString(); }
            set { ForegroundKey = value.ToColor(); }
        }

        [DataMember]
        private string ForegroundSubColor
        {
            get { return ForegroundSub.ToColorString(); }
            set { ForegroundSub = value.ToColor(); }
        }

        #endregion

        [DataMember]
        public HighlightableColorTheme Colors { get; set; }

        [IgnoreDataMember]
        public Color ForegroundKey { get; set; }

        [IgnoreDataMember]
        public Color ForegroundSub { get; set; }

        [DataMember]
        public HighlightableColorTheme FavoriteAndRetweetButton { get; set; }

        [DataMember]
        public HighlightableColorTheme FavoriteButton { get; set; }

        [DataMember]
        public HighlightableColorTheme ColoredFavoriteButton { get; set; }

        [DataMember]
        public HighlightableColorTheme RetweetButton { get; set; }

        [DataMember]
        public HighlightableColorTheme ColoredRetweetButton { get; set; }

        [DataMember]
        public HighlightableColorTheme MentionButton { get; set; }

        [DataMember]
        public HighlightableColorTheme DeleteButton { get; set; }
    }
}
