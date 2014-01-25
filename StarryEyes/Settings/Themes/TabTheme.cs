using System;
using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Media;
using StarryEyes.Annotations;

namespace StarryEyes.Settings.Themes
{
    [DataContract]
    public class TabTheme : IResourceConfigurator, ICloneable
    {
        #region Serialization properties

        [DataMember(Order = 1)]
        private string DefaultColor
        {
            get { return Default.ToColorString(); }
            set { Default = value.ToColor(); }
        }

        [DataMember(Order = 2)]
        private string SelectedColor
        {
            get { return Selected.ToColorString(); }
            set { Selected = value.ToColor(); }
        }

        [DataMember(Order = 3)]
        private string FocusedColor
        {
            get { return Focused.ToColorString(); }
            set { Focused = value.ToColor(); }
        }

        [DataMember(Order = 4)]
        private string UnreadCountColor
        {
            get { return UnreadCount.ToColorString(); }
            set { UnreadCount = value.ToColor(); }
        }

        #endregion

        private FontTheme _tabFont;

        /// <summary>
        /// Tab font
        /// </summary>
        [DataMember(Order = 0), NotNull]
        public FontTheme TabFont
        {
            get { return _tabFont ?? (_tabFont = FontTheme.Default); }
            set { _tabFont = value; }
        }

        /// <summary>
        /// Text color for unselected tabs
        /// </summary>
        [IgnoreDataMember]
        public Color Default { get; set; }

        /// <summary>
        /// Text color for selected tabs
        /// </summary>
        [IgnoreDataMember]
        public Color Selected { get; set; }

        /// <summary>
        /// Text color for focused tabs
        /// </summary>
        [IgnoreDataMember]
        public Color Focused { get; set; }

        /// <summary>
        /// Text color for unread counts
        /// </summary>
        [IgnoreDataMember]
        public Color UnreadCount { get; set; }

        public void ConfigureResourceDictionary(ResourceDictionary dictionary, string prefix)
        {
            TabFont.ConfigureResourceDictionary(dictionary, prefix + "Font");
            Default.ConfigureResourceDictionary(dictionary, prefix);
            Selected.ConfigureResourceDictionary(dictionary, prefix + "Selected");
            Focused.ConfigureResourceDictionary(dictionary, prefix + "Focused");
            UnreadCount.ConfigureResourceDictionary(dictionary, prefix + "UnreadCount");
        }

        public TabTheme Clone()
        {
            return new TabTheme
            {
                TabFont = TabFont.Clone(),
                Default = Default,
                Selected = Selected,
                Focused = Focused,
                UnreadCount = UnreadCount,
            };
        }

        object ICloneable.Clone()
        {
            return Clone();
        }
    }
}
