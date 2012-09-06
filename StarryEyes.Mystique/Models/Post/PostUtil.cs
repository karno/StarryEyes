using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using StarryEyes.Mystique.Helpers;

namespace StarryEyes.Mystique.Models.Post
{
    public static class PostUtil
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
                .SelectMany(s =>
                {
                    if (s.Item2) // URL matched
                        return new[] { s };
                    else
                        return RegexHelper.TwitterUrlCCTLDRegex.Tokenize(s.Item1);
                })
                .Sum(s => s.Item2 ?
                    (s.Item1.StartsWith("https", StringComparison.CurrentCultureIgnoreCase) ?
                    HttpsUrlLength : HttpUrlLength) :
                    s.Item1.Length);
        }
    }
}
