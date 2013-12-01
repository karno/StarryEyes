using System.Runtime.Serialization;
using System.Windows.Media;
using StarryEyes.Annotations;

namespace StarryEyes.Settings.Themes
{
    [DataContract]
    public class TabTheme
    {
        #region Serialization properties

        [DataMember]
        public string DefaultColor
        {
            get { return Default.ToColorString(); }
            set { Default = value.ToColor(); }
        }

        [DataMember]
        public string SelectedColor
        {
            get { return Selected.ToColorString(); }
            set { Selected = value.ToColor(); }
        }

        [DataMember]
        public string FocusedColor
        {
            get { return Focused.ToColorString(); }
            set { Focused = value.ToColor(); }
        }

        [DataMember]
        public string UnreadCountColor
        {
            get { return UnreadCount.ToColorString(); }
            set { UnreadCount = value.ToColor(); }
        }

        #endregion

        private ThemeFont _tabFont;

        [DataMember, NotNull]
        public ThemeFont TabFont
        {
            get { return _tabFont ?? (_tabFont = ThemeFont.Default); }
            set { _tabFont = value; }
        }

        [IgnoreDataMember]
        public Color Default { get; set; }

        [IgnoreDataMember]
        public Color Selected { get; set; }

        [IgnoreDataMember]
        public Color Focused { get; set; }

        [IgnoreDataMember]
        public Color UnreadCount { get; set; }
    }
}
