using System;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;
using StarryEyes.Annotations;

namespace StarryEyes.Settings.Themes
{
    [DataContract, KnownType(typeof(FontFamily))]
    public class FontPreference
    {
        public FontPreference()
            : this(SystemFonts.MessageFontFamily)
        {

        }

        public FontPreference(FontFamily fontFamily)
        {
            this._fontFamily = fontFamily;
        }

        private FontFamily _fontFamily;

        [IgnoreDataMember]
        public static FontPreference Default
        {
            get
            {
                return new FontPreference
                {
                    FontFamily = SystemFonts.MessageFontFamily,
                    FontSize = SystemFonts.MessageFontSize,
                };
            }
        }

        [DataMember]
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

        [IgnoreDataMember, NotNull]
        public FontFamily FontFamily
        {
            get { return _fontFamily; }
            set { _fontFamily = value; }
        }

        [DataMember]
        public double FontSize { get; set; }
    }
}
