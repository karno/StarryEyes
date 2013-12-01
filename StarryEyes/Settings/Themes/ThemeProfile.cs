using System.IO;
using System.Runtime.Serialization;
using System.Xml;
using StarryEyes.Annotations;

namespace StarryEyes.Settings.Themes
{
    [DataContract]
    public class ThemeProfile
    {
        private ThemeFont _globalFont;
        private UserFlipTheme _userFlipColor;
        private TabTheme _tabColor;

        private ThemeProfile() { }

        public ThemeProfile(string name)
        {
            this.Name = name;
        }

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

        public string Name { get; private set; }

        [DataMember, NotNull]
        public ThemeFont GlobalFont
        {
            get { return _globalFont ?? (_globalFont = ThemeFont.Default); }
            set { _globalFont = value; }
        }

        [DataMember]
        public HighlightableColorTheme GlobalColor { get; set; }

        [DataMember]
        public HighlightableColorTheme BackstageColor { get; set; }

        [DataMember]
        public ColorTheme AccountSelectionFlipColor { get; set; }

        [DataMember]
        public HighlightableColorTheme SearchFlipColor { get; set; }

        [DataMember, NotNull]
        public UserFlipTheme UserFlipColor
        {
            get { return _userFlipColor ?? (_userFlipColor = new UserFlipTheme()); }
            set { _userFlipColor = value; }
        }

        [DataMember, NotNull]
        public TabTheme Tab
        {
            get { return _tabColor ?? (_tabColor = new TabTheme()); }
            set { _tabColor = value; }
        }

        [DataMember]
        public TweetTheme TweetDefault { get; set; }

        [DataMember]
        public TweetTheme TweetMyself { get; set; }

        [DataMember]
        public TweetTheme TweetMention { get; set; }

        [DataMember]
        public TweetTheme TweetRetweet { get; set; }

        [DataMember]
        public TweetTheme TweetDirectMessage { get; set; }
    }
}
