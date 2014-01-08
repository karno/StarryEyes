using System;
using System.IO;
using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Media;
using System.Xml;
using StarryEyes.Annotations;

namespace StarryEyes.Settings.Themes
{
    /// <summary>
    /// Describes Theme Profile
    /// </summary>
    [DataContract]
    public class ThemeProfile : ICloneable
    {
        #region Serialization properties

        [DataMember(Name = "GlobalKeyColor")]
        private string GlobalKeyColorString
        {
            get { return this.GlobalKeyColor.ToColorString(); }
            set { this.GlobalKeyColor = value.ToColor(); }
        }

        #endregion

        private FontTheme _globalFont;
        private UserFlipTheme _userFlipColor;
        private TabTheme _tabColor;

        /// <summary>
        /// Internal constructor
        /// </summary>
        private ThemeProfile() { }

        /// <summary>
        /// Construct class with name
        /// </summary>
        /// <param name="name"></param>
        public ThemeProfile(string name)
        {
            this.Name = name;
        }

        /// <summary>
        /// Load from xml file.
        /// </summary>
        /// <param name="path">load path</param>
        /// <returns>loaded profile</returns>
        public static ThemeProfile Load(string path)
        {
            var name = Path.GetFileNameWithoutExtension(path);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("could not load theme: " + path + " - file not found.");
            }
            using (var fs = File.OpenRead(path))
            {
                var dcs = new DataContractSerializer(typeof(ThemeProfile));
                var profile = (ThemeProfile)dcs.ReadObject(fs);
                profile.Name = name;
                return profile;
            }
        }

        /// <summary>
        /// Save theme to xml file.
        /// </summary>
        /// <param name="directory">target directory.(must exist)</param>
        public void Save(string directory)
        {
            var path = Path.Combine(directory, Name + ".xml");
            using (var fs = File.Create(path))
            using (var w = XmlWriter.Create(fs, new XmlWriterSettings { Indent = true }))
            {
                var dcs = new DataContractSerializer(typeof(ThemeProfile));
                dcs.WriteObject(w, this);
            }
        }

        /// <summary>
        /// Theme Name
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Krile Global Font
        /// </summary>
        [DataMember, NotNull]
        public FontTheme GlobalFont
        {
            get { return _globalFont ?? (_globalFont = FontTheme.Default); }
            set { _globalFont = value; }
        }

        /// <summary>
        /// Global base background/foreground color
        /// </summary>
        [DataMember]
        public ThemeColors BaseColor { get; set; }

        /// <summary>
        /// Global key colors (one-point color)
        /// </summary>
        [IgnoreDataMember]
        public Color GlobalKeyColor { get; set; }

        /// <summary>
        /// Colors for title bar.
        /// </summary>
        [DataMember]
        public ThemeColors TitleBarColor { get; set; }

        /// <summary>
        /// Backstage background/foreground color
        /// </summary>
        [DataMember]
        public ThemeColors BackstageColor { get; set; }

        /// <summary>
        /// Account selection flip background/foreground color
        /// </summary>
        [DataMember]
        public ThemeColors AccountSelectionFlipColor { get; set; }

        /// <summary>
        /// Theme description for search-flip
        /// </summary>
        [DataMember]
        public SearchFlipTheme SearchFlipColor { get; set; }

        /// <summary>
        /// Theme description for user-flip
        /// </summary>
        [DataMember, NotNull]
        public UserFlipTheme UserFlipColor
        {
            get { return _userFlipColor ?? (_userFlipColor = new UserFlipTheme()); }
            set { _userFlipColor = value; }
        }

        /// <summary>
        /// Theme description for tabs
        /// </summary>
        [DataMember, NotNull]
        public TabTheme TabColor
        {
            get { return _tabColor ?? (_tabColor = new TabTheme()); }
            set { _tabColor = value; }
        }

        /// <summary>
        /// Theme description for tweets
        /// </summary>
        [DataMember]
        public TweetTheme TweetDefaultColor { get; set; }

        /// <summary>
        /// Theme description for your tweets
        /// </summary>
        [DataMember]
        public TweetTheme TweetMyselfColor { get; set; }

        /// <summary>
        /// Theme description for mentioned tweets
        /// </summary>
        [DataMember]
        public TweetTheme TweetMentionColor { get; set; }

        /// <summary>
        /// Theme description for retweeted tweets
        /// </summary>
        [DataMember]
        public TweetTheme TweetRetweetColor { get; set; }

        /// <summary>
        /// Theme description for direct messages
        /// </summary>
        [DataMember]
        public TweetTheme TweetDirectMessageColor { get; set; }

        /// <summary>
        /// Create theme resource dictionary for adding WPF Theme System
        /// </summary>
        /// <returns>configured resource dictionary</returns>
        public ResourceDictionary CreateResourceDictionary()
        {
            var dict = new ResourceDictionary();
            // Font resource
            GlobalFont.ConfigureResourceDictionary(dict, "Font");
            BaseColor.ConfigureResourceDictionary(dict, "BaseColor");
            GlobalKeyColor.ConfigureResourceDictionary(dict, "GlobalKey");
            TitleBarColor.ConfigureResourceDictionary(dict, "TitleBar");
            BackstageColor.ConfigureResourceDictionary(dict, "Backstage");
            AccountSelectionFlipColor.ConfigureResourceDictionary(dict, "AccountSelectionFlip");
            SearchFlipColor.ConfigureResourceDictionary(dict, "SearchFlip");
            UserFlipColor.ConfigureResourceDictionary(dict, "UserFlip");
            TabColor.ConfigureResourceDictionary(dict, "Tab");
            TweetDefaultColor.ConfigureResourceDictionary(dict, "TweetDefault");
            TweetMyselfColor.ConfigureResourceDictionary(dict, "TweetMyself");
            TweetMentionColor.ConfigureResourceDictionary(dict, "TweetMention");
            TweetRetweetColor.ConfigureResourceDictionary(dict, "TweetRetweet");
            TweetDirectMessageColor.ConfigureResourceDictionary(dict, "TweetMessage");

            return dict;
        }

        /// <summary>
        /// Create clone of me.
        /// </summary>
        /// <returns></returns>
        public ThemeProfile Clone()
        {
            return new ThemeProfile
            {
                Name = Name,
                GlobalFont = this.GlobalFont.Clone(),
                BaseColor = BaseColor,
                GlobalKeyColor = GlobalKeyColor,
                TitleBarColor = TitleBarColor,
                BackstageColor = BackstageColor,
                AccountSelectionFlipColor = AccountSelectionFlipColor,
                SearchFlipColor = SearchFlipColor.Clone(),
                UserFlipColor = UserFlipColor.Clone(),
                TabColor = this.TabColor.Clone(),
                TweetDefaultColor = this.TweetDefaultColor.Clone(),
                TweetMyselfColor = this.TweetMyselfColor.Clone(),
                TweetMentionColor = this.TweetMentionColor.Clone(),
                TweetRetweetColor = this.TweetRetweetColor.Clone(),
                TweetDirectMessageColor = this.TweetDirectMessageColor.Clone()
            };
        }

        object ICloneable.Clone()
        {
            return Clone();
        }
    }
}
