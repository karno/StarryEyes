using System;
using System.Runtime.Serialization;
using System.Windows;

namespace StarryEyes.Settings.Themes
{
    /// <summary>
    /// Describes search flip theme
    /// </summary>
    public class SearchFlipTheme : IResourceConfigurator, ICloneable
    {
        /// <summary>
        /// Default background/foreground colors
        /// </summary>
        [DataMember]
        public ThemeColors Default { get; set; }

        /// <summary>
        /// Colors for foreground/background when query is invalid
        /// </summary>
        [DataMember]
        public ThemeColors QueryInvalid { get; set; }

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
        /// Colors for mouse pressed item
        /// </summary>
        [DataMember]
        public ThemeColors Pressed { get; set; }

        public void ConfigureResourceDictionary(ResourceDictionary dictionary, string prefix)
        {
            Default.ConfigureResourceDictionary(dictionary, prefix);
            QueryInvalid.ConfigureResourceDictionary(dictionary, prefix + "QueryInvalid");
            Selected.ConfigureResourceDictionary(dictionary, prefix + "Selected");
            Hovering.ConfigureResourceDictionary(dictionary, prefix + "Hovering");
            Pressed.ConfigureResourceDictionary(dictionary, prefix + "Pressed");
        }

        public SearchFlipTheme Clone()
        {
            return new SearchFlipTheme
            {
                Default = Default,
                QueryInvalid = QueryInvalid,
                Selected = Selected,
                Hovering = Hovering,
                Pressed = Pressed
            };
        }

        object ICloneable.Clone()
        {
            return Clone();
        }
    }
}