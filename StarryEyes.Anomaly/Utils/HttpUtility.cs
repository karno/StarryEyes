using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace StarryEyes.Anomaly.Utils
{
    public static class HttpUtility
    {
        /// <summary>
        /// Encode string with URL encoding method by UTF8
        /// </summary>
        /// <remarks>
        /// This method will replace space to ' '.
        /// </remarks>
        /// <param name="s">string for encode</param>
        /// <returns>encoded string</returns>
        public static string UrlEncode(string s)
        {
            return UrlEncode(s, Encoding.UTF8);
        }

        /// <summary>
        /// Encode string with URL encoding method
        /// </summary>
        /// <remarks>
        /// This method will replace space to ' '.
        /// </remarks>
        /// <param name="s">string for encode</param>
        /// <param name="enc">using encoding</param>
        /// <returns>encoded string</returns>
        public static string UrlEncode(string s, Encoding enc)
        {
            var rt = new StringBuilder();
            foreach (var i in enc.GetBytes(s))
                if (i == 0x20)
                    rt.Append('+');
                else if (i >= 0x30 && i <= 0x39 || i >= 0x41 && i <= 0x5a || i >= 0x61 && i <= 0x7a)
                    rt.Append((char)i);
                else
                    rt.Append("%" + i.ToString("X2"));
            return rt.ToString();
        }

        const string AllowedChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_.~";

        /// <summary>
        /// Encode URL (with OAuth format)
        /// </summary>
        /// <param name="value">target</param>
        /// <param name="encoding">using encode</param>
        /// <param name="upper">helix cast to upper</param>
        /// <returns>encoded string</returns>
        public static string UrlEncodeStrict(string value, Encoding encoding, bool upper)
        {
            var result = new StringBuilder();
            var data = encoding.GetBytes(value);
            var len = data.Length;

            for (var i = 0; i < len; i++)
            {
                int c = data[i];
                if (c < 0x80 && AllowedChars.IndexOf((char)c) != -1)
                {
                    result.Append((char)c);
                }
                else
                {
                    if (upper)
                        result.Append('%' + String.Format("{0:X2}", (int)data[i]));
                    else
                        result.Append('%' + String.Format("{0:x2}", (int)data[i]));
                }
            }
            return result.ToString();
        }

        /// <summary>
        /// Decode string with URL decoding method by UTF8
        /// </summary>
        /// <param name="s">string for decode</param>
        /// <returns>decoded string</returns>
        public static string UrlDecode(string s)
        {
            return UrlDecode(s, Encoding.UTF8);
        }

        /// <summary>
        /// Decode string with URL decoding method
        /// </summary>
        /// <param name="s">string for decode</param>
        /// <param name="enc">using encoding</param>
        /// <returns>decoded string</returns>
        public static string UrlDecode(string s, Encoding enc)
        {
            var bytes = new List<byte>();
            for (var i = 0; i < s.Length; i++)
            {
                var c = s[i];
                switch (c)
                {
                    case '%':
                        bytes.Add(
                            (byte)
                            int.Parse(
                                s[++i].ToString(CultureInfo.InvariantCulture) +
                                s[++i].ToString(CultureInfo.InvariantCulture), NumberStyles.HexNumber));
                        break;
                    case '+':
                        bytes.Add(0x20);
                        break;
                    default:
                        bytes.Add((byte)c);
                        break;
                }
            }
            return enc.GetString(bytes.ToArray(), 0, bytes.Count);
        }
    }

    public static class ImageUriUtility
    {
        private const string HostDomain = "twimg.com";

        public static Uri ChangeImageSize(this Uri imageUri, ImageSize size)
        {
            if (imageUri == null || !imageUri.Host.EndsWith(HostDomain))
            {
                return imageUri;
            }
            var str = imageUri.ToString();
            int extpos;
            if ((extpos = str.LastIndexOf('.')) < 0 ||
                str.Length - extpos > 10) // extension is too long
            {
                return imageUri;
            }
            // stash ext
            var ext = str.Substring(extpos);
            // normalize url
            var body = MakeOriginal(str.Substring(0, extpos));
            switch (size)
            {
                case ImageSize.Normal:
                    body += "_normal";
                    break;
                case ImageSize.Bigger:
                    body += "_bigger";
                    break;
                case ImageSize.Mini:
                    body += "_mini";
                    break;
            }
            try
            {
                return new Uri(body + ext);
            }
            catch (UriFormatException)
            {
                return imageUri;
            }
        }

        private static string MakeOriginal(string uriStr)
        {
            if (uriStr.Length < 7) return uriStr;
            if (uriStr[uriStr.Length - 7] == '_')
            {
                // bigger, normal
                return uriStr.Substring(0, uriStr.Length - 7);
            }
            if (uriStr[uriStr.Length - 5] == '_')
            {
                // mini
                return uriStr.Substring(0, uriStr.Length - 5);
            }
            // original
            return uriStr;
        }
    }

    public enum ImageSize
    {
        /// <summary>
        /// normal size, 48x48
        /// </summary>
        Normal,
        /// <summary>
        /// larger size, 73x73
        /// </summary>
        Bigger,
        /// <summary>
        /// Mini size, 24x24
        /// </summary>
        Mini,
        /// <summary>
        /// Original size, high resolution
        /// </summary>
        Original
    }
}
