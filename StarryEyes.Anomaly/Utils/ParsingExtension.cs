using System;
using System.Xml.Linq;

namespace StarryEyes.Anomaly.Utils
{
    /// <summary>
    /// XML Node Parser
    /// </summary>
    public static class ParsingExtension
    {
        public const string TwitterDateTimeFormat = "ddd MMM d HH':'mm':'ss zzz yyyy";

        #region for String

        public static bool ParseBool(this string s, bool def)
        {
            if (s == null)
            {
                return def;
            }
            return def ? s.ToLower() != "false" : s.ToLower() == "true";
        }

        public static long ParseLong(this string s)
        {
            long v;
            return long.TryParse(s, out v) ? v : 0;
        }

        public static long? ParseNullableId(this string s)
        {
            long v;
            return long.TryParse(s, out v) && v != 0 ? v : (long?)null;
        }


        public static DateTime ParseDateTime(this string s)
        {
            DateTime dt;
            return DateTime.TryParse(s, out dt) ? dt : DateTime.MinValue;
        }

        public static DateTime ParseDateTime(this string s, string format)
        {
            DateTime dt;
            return DateTime.TryParseExact(s,
                format,
                System.Globalization.DateTimeFormatInfo.InvariantInfo,
                System.Globalization.DateTimeStyles.None, out dt) ? dt : DateTime.MinValue;
        }

        public static DateTime ParseUnixTime(this string s)
        {
            if (s == null) return DateTime.MinValue;
            return UnixEpoch.GetDateTimeByUnixEpoch(s.ParseLong());
        }

        public static TimeSpan ParseUtcOffset(this string s)
        {
            int seconds;
            int.TryParse(s, out seconds);
            return new TimeSpan(0, 0, seconds);
        }

        public static Uri ParseUri(this string s)
        {
            Uri ret;
            if (Uri.TryCreate(s, UriKind.RelativeOrAbsolute, out ret))
                return ret;
            return null;
        }

        public static Uri ParseUriAbsolute(this string s)
        {
            Uri ret = s.ParseUri();
            if (ret == null || !ret.IsAbsoluteUri) return null;
            return ret;
        }

        #endregion

        #region for Element

        public static string ParseString(this XElement e)
        {
            return e == null ? null : e.Value.Replace("&lt;", "<").Replace("&gt;", ">");
        }

        public static bool ParseBool(this XElement e, bool def = false)
        {
            return ParseBool(e == null ? null : e.Value, def);
        }

        public static long ParseLong(this XElement e)
        {
            return ParseLong(e == null ? null : e.Value);
        }

        public static DateTime ParseDateTime(this XElement e)
        {
            return ParseDateTime(e == null ? null : e.Value);
        }

        public static DateTime ParseDateTime(this XElement e, string format)
        {
            return ParseDateTime(e == null ? null : e.Value, format);
        }

        public static DateTime ParseUnixTime(this XElement e)
        {
            return ParseUnixTime(e == null ? null : e.Value);
        }

        public static TimeSpan ParseUtcOffset(this XElement e)
        {
            return ParseUtcOffset(e == null ? null : e.Value);
        }

        public static Uri ParseUri(this XElement e)
        {
            return e.ParseString().ParseUri();
        }

        #endregion

        #region for Attributes

        public static string ParseString(this XAttribute e)
        {
            return e == null ? null : e.Value.Replace("&lt;", "<").Replace("&gt;", ">");
        }

        public static bool ParseBool(this XAttribute e, bool def = false)
        {
            return ParseBool(e == null ? null : e.Value, def);
        }

        public static long ParseLong(this XAttribute e)
        {
            return ParseLong(e == null ? null : e.Value);
        }

        public static DateTime ParseDateTime(this XAttribute e)
        {
            return ParseDateTime(e == null ? null : e.Value);
        }

        public static DateTime ParseDateTime(this XAttribute e, string format)
        {
            return ParseDateTime(e == null ? null : e.Value, format);
        }

        public static DateTime ParseUnixTime(this XAttribute e)
        {
            return ParseUnixTime(e == null ? null : e.Value);
        }

        public static TimeSpan ParseUtcOffset(this XAttribute e)
        {
            return ParseUtcOffset(e == null ? null : e.Value);
        }

        public static Uri ParseUri(this XAttribute e)
        {
            var uri = e.ParseString();
            try
            {
                if (String.IsNullOrEmpty(uri))
                    return null;
                return new Uri(uri);
            }
            catch (UriFormatException)
            {
                return null;
            }
        }

        #endregion

        #region for XText

        public static string ParseString(this XText e)
        {
            return e == null ? null : e.Value.Replace("&lt;", "<").Replace("&gt;", ">");
        }

        public static bool ParseBool(this XText e, bool def = false)
        {
            return ParseBool(e == null ? null : e.Value, def);
        }

        public static long ParseLong(this XText e)
        {
            return ParseLong(e == null ? null : e.Value);
        }

        public static DateTime ParseDateTime(this XText e)
        {
            return ParseDateTime(e == null ? null : e.Value);
        }

        public static DateTime ParseDateTime(this XText e, string format)
        {
            return ParseDateTime(e == null ? null : e.Value, format);
        }

        public static DateTime ParseUnixTime(this XText e)
        {
            return ParseUnixTime(e == null ? null : e.Value);
        }

        public static TimeSpan ParseUtcOffset(this XText e)
        {
            return ParseUtcOffset(e == null ? null : e.Value);
        }

        public static Uri ParseUri(this XText e)
        {
            var uri = e.ParseString();
            try
            {
                if (String.IsNullOrEmpty(uri))
                    return null;
                return new Uri(uri);
            }
            catch (UriFormatException)
            {
                return null;
            }
        }

        #endregion

        #region for General Text

        public static string ResolveEntity(string text)
        {
            return text
                // .Replace("&quot;", "\"")
                .Replace("&lt;", "<")
                .Replace("&gt;", ">")
                .Replace("&amp;", "&");
        }

        public static string EscapeEntity(string text)
        {
            return text
                .Replace("&", "&amp;")
                .Replace(">", "&gt;")
                .Replace("<", "&lt;");
            // .Replace("\"", "&quot;")
        }

        #endregion
    }
}

