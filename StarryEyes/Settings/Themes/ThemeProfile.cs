using System.IO;
using System.Runtime.Serialization;
using System.Xml;
using StarryEyes.Annotations;

namespace StarryEyes.Settings.Themes
{
    [DataContract]
    public class ThemeProfile
    {
        private FontPreference _globalFont;
        private UserFlipColorPreference _userFlipColor;
        private TabPreference _tabColor;

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
        public FontPreference GlobalFont
        {
            get { return _globalFont ?? (_globalFont = FontPreference.Default); }
            set { _globalFont = value; }
        }

        [DataMember]
        public HighlightableColorPreference GlobalColor { get; set; }

        [DataMember]
        public HighlightableColorPreference BackstageColor { get; set; }

        [DataMember]
        public ColorPreference AccountSelectionFlipColor { get; set; }

        [DataMember]
        public HighlightableColorPreference SearchFlipColor { get; set; }

        [DataMember, NotNull]
        public UserFlipColorPreference UserFlipColor
        {
            get { return _userFlipColor ?? (_userFlipColor = new UserFlipColorPreference()); }
            set { _userFlipColor = value; }
        }

        [DataMember, NotNull]
        public TabPreference Tab
        {
            get { return _tabColor ?? (_tabColor = new TabPreference()); }
            set { _tabColor = value; }
        }

        [DataMember]
        public TweetColorPreference TweetDefault { get; set; }

        [DataMember]
        public TweetColorPreference TweetMyself { get; set; }

        [DataMember]
        public TweetColorPreference TweetMention { get; set; }

        [DataMember]
        public TweetColorPreference TweetRetweet { get; set; }

        [DataMember]
        public TweetColorPreference TweetDirectMessage { get; set; }
    }
}
