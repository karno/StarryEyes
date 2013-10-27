using System;
using System.Collections.Generic;
using System.Linq;
using StarryEyes.Albireo;
using StarryEyes.Anomaly.Utils;
using StarryEyes.Helpers;
using StarryEyes.Models.Subsystems;

namespace StarryEyes.Models
{
    public static class StatusTextUtil
    {
        public static int CountText(string text)
        {
            var formatted = ParsingExtension.EscapeEntity(text.Replace("\r\n", "\n"));
            var replaced = TwitterRegexPatterns.ValidUrl.Replace(
                formatted,
                m => m.Groups[TwitterRegexPatterns.ValidUrlGroupBefore] + "<>" +
                     m.Groups[TwitterRegexPatterns.ValidUrlGroupUrl] + "<");
            return replaced.Split(new[] { "<" }, StringSplitOptions.RemoveEmptyEntries)
                           .Select(s => new { Text = s, IsUrl = s.StartsWith(">") })
                           .Select(s =>
                           {
                               if (s.IsUrl)
                               {
                                   return s.Text.Substring(1)
                                           .StartsWith("https", StringComparison.CurrentCultureIgnoreCase)
                                              ? TwitterConfigurationService.HttpsUrlLength
                                              : TwitterConfigurationService.HttpUrlLength;
                               }
                               return ParsingExtension.ResolveEntity(s.Text).Length;
                           })
                           .Sum();
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

        /// <summary>
        /// 文字列をトークン化します。
        /// </summary>
        public static IEnumerable<TextToken> Tokenize(string raw)
        {
            if (String.IsNullOrEmpty(raw)) yield break;
            var escaped = ParsingExtension.EscapeEntity(raw);
            escaped = TwitterRegexPatterns.ValidUrl.Replace(escaped, m =>
            {
                // # => &sharp; (ハッシュタグで再識別されることを防ぐ)
                var repl = m.Groups[TwitterRegexPatterns.ValidUrlGroupUrl].Value.Replace("#", "&sharp;");
                return m.Groups[TwitterRegexPatterns.ValidUrlGroupBefore] + "<U>" + repl + "<";
            });
            escaped = TwitterRegexPatterns.ValidMentionOrList.Replace(
                escaped,
                m => m.Groups[TwitterRegexPatterns.ValidMentionOrListGroupBefore].Value +
                     m.Groups[TwitterRegexPatterns.ValidMentionOrListGroupAt] +
                     "<A>" + m.Groups[TwitterRegexPatterns.ValidMentionOrListGroupUsername].Value +
                     m.Groups[TwitterRegexPatterns.ValidMentionOrListGroupList].Value + "<");
            escaped = TwitterRegexPatterns.ValidHashtag.Replace(escaped, m => m.Groups[1] + "<H>" + m.Groups[1].Value + "<");
            var splitted = escaped.Split(new[] { '<' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var s in splitted)
            {
                if (s.Contains('>'))
                {
                    var kind = s[0];
                    var body = ParsingExtension.ResolveEntity(s.Substring(2));
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
                    yield return new TextToken(TokenKind.Text, ParsingExtension.ResolveEntity(s));
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
