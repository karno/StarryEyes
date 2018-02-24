using System;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;
using JetBrains.Annotations;

namespace StarryEyes.Settings.Themes
{
    /// <summary>
    /// Describe font theme
    /// </summary>
    [DataContract, KnownType(typeof(FontFamily))]
    public class FontTheme : IResourceConfigurator, ICloneable
    {
        /// <summary>
        /// Private constructor
        /// </summary>
        private FontTheme()
        {
        }

        /// <summary>
        /// Construct class with parameter.
        /// </summary>
        /// <param name="fontFamily">font family</param>
        /// <param name="fontSize">font size</param>
        public FontTheme([CanBeNull] FontFamily fontFamily, double fontSize)
        {
            if (fontFamily == null) throw new ArgumentNullException("fontFamily");
            this._fontFamily = fontFamily;
            this.FontSize = fontSize;
        }

        private FontFamily _fontFamily;

        /// <summary>
        /// The default font theme.
        /// </summary>
        [IgnoreDataMember]
        public static FontTheme Default
        {
            get
            {
                return new FontTheme
                {
                    FontFamily = SystemFonts.MessageFontFamily,
                    FontSize = SystemFonts.MessageFontSize,
                };
            }
        }

        public static FontTheme CreateDefaultWithSize(double size)
        {
            var df = Default;
            df.FontSize = size;
            return df;
        }

        /// <summary>
        /// Name of font family, for serialization
        /// </summary>
        [DataMember(Order = 0)]
        private string FontFamilyName
        {
            get
            {
                var culture = XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag);
                return _fontFamily.FamilyNames.ContainsKey(culture)
                    ? _fontFamily.FamilyNames[culture]
                    : _fontFamily.FamilyNames.Select(l => l.Value).FirstOrDefault();
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException();
                }
                _fontFamily = new FontFamily(value);
            }
        }

        /// <summary>
        /// Font family
        /// </summary>
        [IgnoreDataMember, CanBeNull]
        public FontFamily FontFamily
        {
            get { return _fontFamily; }
            set { _fontFamily = value; }
        }

        /// <summary>
        /// Font size
        /// </summary>
        [DataMember(Order = 1)]
        public double FontSize { get; set; }

        public void ConfigureResourceDictionary(ResourceDictionary dictionary, string prefix)
        {
            dictionary[prefix + "FontFamily"] = FontFamily;
            dictionary[prefix + "FontSize"] = FontSize;
        }

        /// <summary>
        /// Create clone
        /// </summary>
        [CanBeNull]
        public FontTheme Clone()
        {
            return new FontTheme
            {
                FontFamilyName = FontFamilyName,
                FontSize = FontSize
            };
        }

        object ICloneable.Clone()
        {
            return Clone();
        }
    }
}