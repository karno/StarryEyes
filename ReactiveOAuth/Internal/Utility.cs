using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
#if WINDOWS_PHONE
using Microsoft.Phone.Reactive;
#endif

namespace Codeplex.OAuth
{
    internal static class Utility
    {
        static readonly DateTime unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        public static long ToUnixTime(this DateTime target)
        {
            return (long)(target - unixEpoch).TotalSeconds;
        }

        /// <summary>Escape RFC3986 String</summary>
        public static string UrlEncode(this string stringToEscape)
        {
            return Uri.EscapeDataString(stringToEscape)
                .Replace("!", "%21")
                .Replace("*", "%2A")
                .Replace("'", "%27")
                .Replace("(", "%28")
                .Replace(")", "%29");
        }

        public static string Wrap(this string input, string wrapper)
        {
            return wrapper + input + wrapper;
        }

        public static string ToString<T>(this IEnumerable<T> source, string separator)
        {
            var index = 0;
            return source.Aggregate(new StringBuilder(),
                    (sb, o) => (index++ == 0) ? sb.Append(o) : sb.AppendFormat("{0}{1}", separator, o))
                .ToString();
        }
    }
}