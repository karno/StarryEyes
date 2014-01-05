using System;
using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Media;

namespace StarryEyes.Settings.Themes
{
    /// <summary>
    /// Describes user flip theme
    /// </summary>
    [DataContract]
    public class UserFlipTheme : IResourceConfigurator, ICloneable
    {
        #region Serialization properties

        [DataMember]
        private string NotFollowedColor
        {
            get { return NotFollowed.ToColorString(); }
            set { NotFollowed = value.ToColor(); }
        }

        [DataMember]
        private string NotFollowbackedColor
        {
            get { return NotFollowbacked.ToColorString(); }
            set { NotFollowbacked = value.ToColor(); }
        }

        [DataMember]
        private string FollowingColor
        {
            get { return Following.ToColorString(); }
            set { Following = value.ToColor(); }
        }

        [DataMember]
        private string FollowbackedColor
        {
            get { return Followbacked.ToColorString(); }
            set { Followbacked = value.ToColor(); }
        }

        [DataMember]
        private string BlockingColor
        {
            get { return Blocking.ToColorString(); }
            set { Blocking = value.ToColor(); }
        }

        #endregion

        /// <summary>
        /// Default foreground/background color
        /// </summary>
        [DataMember]
        public ThemeColors Default { get; set; }

        /// <summary>
        /// Colors for selected item
        /// </summary>
        [DataMember]
        public ThemeColors Selected { get; set; }

        /// <summary>
        /// Colors for mouse hovered item
        /// </summary>
        [DataMember]
        public ThemeColors Hovering { get; set; }

        /// <summary>
        /// Colors for pressed item
        /// </summary>
        [DataMember]
        public ThemeColors Pressed { get; set; }

        /// <summary>
        /// Text color for not followbacked label
        /// </summary>
        [IgnoreDataMember]
        public Color NotFollowbacked { get; set; }

        /// <summary>
        /// Text color for followbacked label
        /// </summary>
        [IgnoreDataMember]
        public Color Followbacked { get; set; }

        /// <summary>
        /// Button color when not followed
        /// </summary>
        [IgnoreDataMember]
        public Color NotFollowed { get; set; }

        /// <summary>
        /// Button color when following
        /// </summary>
        [IgnoreDataMember]
        public Color Following { get; set; }

        /// <summary>
        /// Button color when blocking
        /// </summary>
        [IgnoreDataMember]
        public Color Blocking { get; set; }

        public void ConfigureResourceDictionary(ResourceDictionary dictionary, string prefix)
        {
            Default.ConfigureResourceDictionary(dictionary, prefix);
            Selected.ConfigureResourceDictionary(dictionary, prefix + "Selected");
            Hovering.ConfigureResourceDictionary(dictionary, prefix + "Hovering");
            Pressed.ConfigureResourceDictionary(dictionary, prefix + "Pressed");

            NotFollowbacked.ConfigureResourceDictionary(dictionary, prefix + "NotFollowbacked");
            Followbacked.ConfigureResourceDictionary(dictionary, prefix + "Followbacked");

            NotFollowed.ConfigureResourceDictionary(dictionary, prefix + "NotFollowed");
            Following.ConfigureResourceDictionary(dictionary, prefix + "Following");
            Blocking.ConfigureResourceDictionary(dictionary, prefix + "Blocking");
        }

        public UserFlipTheme Clone()
        {
            return new UserFlipTheme
            {
                Default = Default,
                Selected = Selected,
                Hovering = Hovering,
                Pressed = Pressed,
                NotFollowbacked = NotFollowbacked,
                Followbacked = Followbacked,
                NotFollowed = NotFollowed,
                Following = Following,
                Blocking = Blocking
            };
        }

        object ICloneable.Clone()
        {
            return Clone();
        }
    }
}
