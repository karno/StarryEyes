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
            dictionary[prefix + "Brush"] = new SolidColorBrush(color).ToFrozen();
        }
    }

    /// <summary>
    /// Describe background/foreground color
    /// </summary>
    [DataContract]
    public class ThemeColors : IResourceConfigurator, ICloneable
    {
        #region serialization properties
        [DataMember(Order = 0)]
        private string BackgroundColor
        {
            get { return Background.ToColorString(); }
            set { Background = value.ToColor(); }
        }

        [DataMember(Order = 1)]
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

        public ThemeColors Clone()
        {
            return new ThemeColors
            {
                Background = Background,
                Foreground = Foreground,
            };
        }

        object ICloneable.Clone()
        {
            return Clone();
        }
    }

    /// <summary>
    /// Describe colors for mouse controllable controls
    /// </summary>
    [DataContract]
    public class ControlColors : IResourceConfigurator, ICloneable
    {
        /// <summary>
        /// Default colors
        /// </summary>
        [DataMember(Order = 0)]
        public ThemeColors Default { get; set; }

        /// <summary>
        /// Mouse hovered colors
        /// </summary>
        [DataMember(Order = 1)]
        public ThemeColors Hovering { get; set; }

        /// <summary>
        /// Pressed colors
        /// </summary>
        [DataMember(Order = 2)]
        public ThemeColors Pressed { get; set; }

        public void ConfigureResourceDictionary(ResourceDictionary dictionary, string prefix)
        {
            Default.ConfigureResourceDictionary(dictionary, prefix);
            Hovering.ConfigureResourceDictionary(dictionary, prefix + "Hovering");
            Pressed.ConfigureResourceDictionary(dictionary, prefix + "Pressed");
        }

        public ControlColors Clone()
        {
            return new ControlColors
            {
                Default = Default.Clone(),
                Hovering = Hovering.Clone(),
                Pressed = Pressed.Clone(),
            };
        }

        object ICloneable.Clone()
        {
            return Clone();
        }
    }
}
