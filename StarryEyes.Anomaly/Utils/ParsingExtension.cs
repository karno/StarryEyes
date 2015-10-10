using System;
using JetBrains.Annotations;

namespace StarryEyes.Anomaly.Utils
{
    /// <summary>
    /// XML Node Parser
    /// </summary>
    public static class ParsingExtension
    {
        public const string TwitterDateTimeFormat = "ddd MMM d HH':'mm':'ss zzz yyyy";

        /// <summary>
        /// Parse text as bool
        /// </summary>
        /// <param name="s">convert value</param>
        /// <param name="@default">default value if string is null or unacceptable value</param>
        /// <returns>converted value</returns>
        public static bool ParseBool([CanBeNull] this string s, bool @default = false)
        {
            if (s == null)
            {
                return @default;
            }
            return @default ? s.ToLower() != "false" : s.ToLower() == "true";
        }

        /// <summary>
        /// Parse string as long
        /// </summary>
        public static long ParseLong([CanBeNull] this string s)
        {
            long v;
            return long.TryParse(s, out v) ? v : 0;
        }

        /// <summary>
        /// Parse nullable id
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static long? ParseNullableId([CanBeNull] this string s)
        {
            long v;
            if (s != null && Int64.TryParse(s, out v) && v != 0)
            {
                return v;
            }
            return null;
        }

        /// <summary>
        /// Parse date time
        /// </summary>
        public static DateTime ParseDateTime([CanBeNull] this string s)
        {
            return s.ParseDateTime(DateTime.MinValue);
        }


        /// <summary>
        /// Parse date time
        /// </summary>
        public static DateTime ParseDateTime([CanBeNull] this string s, DateTime @default)
        {
            DateTime dt;
            if (s != null && DateTime.TryParse(s, out dt))
            {
                return dt;
            }
            return @default;
        }

        /// <summary>
        /// Parse date time
        /// </summary>
        public static DateTime ParseDateTime([CanBeNull] this string s, [NotNull] string format)
        {
            if (format == null) throw new ArgumentNullException(nameof(format));

            return s.ParseDateTime(format, DateTime.MinValue);
        }

        /// <summary>
        /// Parse date time
        /// </summary>
        public static DateTime ParseDateTime([CanBeNull] this string s,
            [NotNull] string format, DateTime @default)
        {
            if (format == null) throw new ArgumentNullException(nameof(format));

            DateTime dt;
            if (s != null &&
                DateTime.TryParseExact(s, format,
                    System.Globalization.DateTimeFormatInfo.InvariantInfo,
                    System.Globalization.DateTimeStyles.None, out dt))
            {
                return dt;
            }
            return @default;
        }

        /// <summary>
        /// Parse date time by twitter default format
        /// </summary>
        public static DateTime ParseTwitterDateTime([CanBeNull] this string s)
        {
            return s.ParseDateTime(TwitterDateTimeFormat);
        }

        /// <summary>
        /// Parse string as unix serial time
        /// </summary>
        public static DateTime ParseUnixTime([CanBeNull] this string s)
        {
            if (s == null) return DateTime.MinValue;
            return UnixEpoch.GetDateTimeByUnixEpoch(s.ParseLong());
        }

        /// <summary>
        /// Parse uri
        /// </summary>
        public static Uri ParseUri([CanBeNull] this string s)
        {
            Uri ret;
            if (s != null && Uri.TryCreate(s, UriKind.RelativeOrAbsolute, out ret))
            {
                return ret;
            }
            return null;
        }

        /// <summary>
        /// Parse uri as absolute uri
        /// </summary>
        public static Uri ParseUriAbsolute([CanBeNull] this string s)
        {
            var ret = s.ParseUri();
            if (ret == null || !ret.IsAbsoluteUri)
            {
                return null;
            }
            return ret;
        }

        /// <summary>
        /// Resolve entity-escaped string
        /// </summary>
        public static string ResolveEntity([CanBeNull] string text)
        {
            return text
                // .Replace("&quot;", "\"")
                ?.Replace("&lt;", "<")
                 .Replace("&gt;", ">")
                 .Replace("&amp;", "&");
        }

        /// <summary>
        /// Escape string with entities
        /// </summary>
        public static string EscapeEntity([CanBeNull] string text)
        {
            return text
                ?.Replace("&", "&amp;")
                 .Replace(">", "&gt;")
                 .Replace("<", "&lt;");
            // .Replace("\"", "&quot;")
        }
    }
}
