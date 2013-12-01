using System;
using System.Runtime.Serialization;
using System.Windows.Media;

namespace StarryEyes.Settings.Themes
{
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
    }


    [DataContract]
    public struct ColorPreference
    {
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

        [IgnoreDataMember]
        public Color Background { get; set; }

        [IgnoreDataMember]
        public Color Foreground { get; set; }
    }

    [DataContract]
    public struct HighlightableColorPreference
    {
        [DataMember]
        public ColorPreference Default { get; set; }

        [DataMember]
        public ColorPreference Highlight { get; set; }
    }
}
