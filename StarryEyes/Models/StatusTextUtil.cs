using System;
using System.Collections.Generic;
using System.Linq;
using StarryEyes.Albireo;
using StarryEyes.Helpers;
using StarryEyes.Models.Subsystems;

namespace StarryEyes.Models
{
    public static class StatusTextUtil
    {
        public static int CountText(string text)
        {
            return TwitterRegexPatterns.ValidUrl
                                       .Tokenize(text.Replace("\r\n", "\n"))
                                       .Sum(s => s.Item2
                                                     ? (s.Item1.StartsWith("https",
                                                                           StringComparison.CurrentCultureIgnoreCase)
                                                            ? TwitterConfigurationService.HttpsUrlLength
                                                            : TwitterConfigurationService.HttpUrlLength)
                                                     : s.Item1.Length);
        }

        public static string AutoEscape(string text)
        {
            return TwitterRegexPatterns.ValidUrl.Replace(text, match =>
            {
                if (match.Groups[TwitterRegexPatterns.ValidUrlGroupProtocol].Value.IsNullOrEmpty())
                    return match.Groups[TwitterRegexPatterns.ValidUrlGroupBefore].Value +
                           EscapeCore(match.Groups[TwitterRegexPatterns.ValidUrlGroupDomain].Value) +
                           match.Groups[TwitterRegexPatterns.ValidUrlGroupPort] +
                           match.Groups[TwitterRegexPatterns.ValidUrlGroupPath] +
                           match.Groups[TwitterRegexPatterns.ValidUrlGroupQueryString];
                return match.Value;
            });
        }

        private static string EscapeCore(string domain)
        {
            var pidx1 = domain.LastIndexOf('.');
            if (pidx1 <= 0) return domain; // invalid data
            var pidx2 = domain.LastIndexOf('.', pidx1 - 1);
            return domain.Insert(pidx2 <= 0 ? pidx1 : pidx2, "\u200c");
        }

        private static string InternalEscape(string raw)
        {
            return raw.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
        }

        private static string InternalUnescape(string escaped)
        {
            return escaped.Replace("&quot;", "\"").Replace("&lt;", "<").Replace("&gt;", ">").Replace("&amp;", "&");
        }

        /// <summary>
        /// 文字列をトークン化します。
        /// </summary>
        public static IEnumerable<TextToken> Tokenize(string raw)
        {
            if (String.IsNullOrEmpty(raw)) yield break;
            var escaped = InternalEscape(raw);
            escaped = TwitterRegexPatterns.ValidUrl.Replace(escaped, m =>
            {
                // # => &sharp; (ハッシュタグで再識別されることを防ぐ)
                var repl = m.Groups[1].Value.Replace("#", "&sharp;");
                return "<U>" + repl + "<";
            });
            escaped = TwitterRegexPatterns.ValidMentionOrList.Replace(escaped, m => "<A>" + m.Value + "<");
            escaped = TwitterRegexPatterns.ValidHashtag.Replace(escaped, m => "<H>" + m.Value + "<");
            var splitted = escaped.Split(new[] { '<' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var s in splitted)
            {
                if (s.Contains('>'))
                {
                    var kind = s[0];
                    var body = InternalUnescape(s.Substring(2));
                    switch (kind)
                    {
                        case 'U':
                            // &sharp; => #
                            yield return new TextToken(TokenKind.Url, body.Replace("&sharp;", "#"));
                            break;
                        case 'A':
                            yield return new TextToken(TokenKind.AtLink, body);
                            break;
                        case 'H':
                            yield return new TextToken(TokenKind.Hashtag, body);
                            break;
                        default:
                            throw new InvalidOperationException("invalid grouping:" + kind.ToString());
                    }
                }
                else
                {
                    yield return new TextToken(TokenKind.Text, InternalUnescape(s));
                }
            }
        }
    }

    public class TextToken
    {
        public TextToken(TokenKind tknd, string tkstr)
        {
            Kind = tknd;
            Text = tkstr;
        }

        public TokenKind Kind { get; set; }

        public string Text { get; set; }
    }

    public enum TokenKind
    {
        Text,
        Url,
        Hashtag,
        AtLink,
    }
}
