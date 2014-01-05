using System;
using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Media;

namespace StarryEyes.Settings.Themes
{
    /// <summary>
    /// Extension methods about colors
    /// </summary>
    internal static class ColorConvertExtension
    {
        public static string ToColorString(this Color color)
        {
            return String.Format("#{0:X2}{1:X2}{2:X2}{3:X2}", color.A, color.R, color.G, color.B);
        }

        public static Color ToColor(this string str)
        {
            return (Color)(ColorConverter.ConvertFromString(str) ?? new Color());
        }

        public static void ConfigureResourceDictionary(this Color color,
            ResourceDictionary dictionary, string prefix)
        {
            dictionary[prefix] = color;
            dictionary[prefix + "Brush"] = new SolidColorBrush(color);
        }
    }

    /// <summary>
    /// Describe background/foreground color
    /// </summary>
    [DataContract]
    public struct ThemeColors : IResourceConfigurator
    {
        #region serialization properties
        [DataMember]
        private string BackgroundColor
        {
            get { return Background.ToColorString(); }
            set { Background = value.ToColor(); }
        }

        [DataMember]
        private string ForegroundColor
        {
            get { return Foreground.ToColorString(); }
            set { Foreground = value.ToColor(); }
        }
        #endregion

        /// <summary>
        /// Background color
        /// </summary>
        [IgnoreDataMember]
        public Color Background { get; set; }

        /// <summary>
        /// Foreground color
        /// </summary>
        [IgnoreDataMember]
        public Color Foreground { get; set; }

        public void ConfigureResourceDictionary(ResourceDictionary dictionary, string prefix)
        {
            Background.ConfigureResourceDictionary(dictionary, prefix + "Background");
            Foreground.ConfigureResourceDictionary(dictionary, prefix + "Foreground");
        }
    }

    /// <summary>
    /// Describe colors for mouse controllable controls
    /// </summary>
    [DataContract]
    public struct ControlColors : IResourceConfigurator
    {
        /// <summary>
        /// Default colors
        /// </summary>
        [DataMember]
        public ThemeColors Default { get; set; }

        /// <summary>
        /// Mouse hovered colors
        /// </summary>
        [DataMember]
        public ThemeColors Hovering { get; set; }

        /// <summary>
        /// Pressed colors
        /// </summary>
        [DataMember]
        public ThemeColors Pressed { get; set; }

        public void ConfigureResourceDictionary(ResourceDictionary dictionary, string prefix)
        {
            Default.ConfigureResourceDictionary(dictionary, prefix);
            Hovering.ConfigureResourceDictionary(dictionary, prefix + "Hovering");
            Pressed.ConfigureResourceDictionary(dictionary, prefix + "Pressed");
        }
    }
}
