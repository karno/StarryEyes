using System;
using System.Linq;
using StarryEyes.Mystique.Helpers;

namespace StarryEyes.Mystique.Models
{
    public static class StatusTextUtil
    {
        public const int MaxTextLength = 140;

        public const int HttpUrlLength = 20; // http
        public const int HttpsUrlLength = HttpUrlLength + 1; // https

        public static int CountText(string text)
        {
            return RegexHelper.UrlRegex.Tokenize(text)
                .SelectMany(s =>
                {
                    if (s.Item2) // URL matched
                        return new[] { s };
                    else
                        return RegexHelper.TwitterUrlTLDRegex.Tokenize(s.Item1);
                })
                .Sum(s => s.Item2 ?
                    (s.Item1.StartsWith("https", StringComparison.CurrentCultureIgnoreCase) ?
                    HttpsUrlLength : HttpUrlLength) :
                    s.Item1.Length);
        }

        public static string AutoEscape(string text)
        {
            return RegexHelper.UrlRegex.Tokenize(text)
                .Select(s =>
                {
                    if (s.Item2) // URL matched
                        return s;
                    else
                        return Tuple.Create(
                            RegexHelper.TwitterUrlTLDRegex.Replace(s.Item1,
                            match => match.Groups[1].Value + " " + match.Groups[2].Value
                            ), true);
                })
                .Select(s => s.Item1)
                .JoinString("");
        }
    }
}
