using System.Runtime.Serialization;
using System.Windows.Media;

namespace StarryEyes.Settings.Themes
{
    [DataContract]
    public struct TweetColorPreference
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
        public HighlightableColorPreference Colors { get; set; }

        [IgnoreDataMember]
        public Color ForegroundKey { get; set; }

        [IgnoreDataMember]
        public Color ForegroundSub { get; set; }

        [DataMember]
        public HighlightableColorPreference FavAndRetweet { get; set; }

        [DataMember]
        public HighlightableColorPreference Favorite { get; set; }

        [DataMember]
        public HighlightableColorPreference FavoriteHighlight { get; set; }

        [DataMember]
        public HighlightableColorPreference Retweet { get; set; }

        [DataMember]
        public HighlightableColorPreference RetweetHighlight { get; set; }

        [DataMember]
        public HighlightableColorPreference Mention { get; set; }
    }
}
