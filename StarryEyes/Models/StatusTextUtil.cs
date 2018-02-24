using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Cadena.Util;
using StarryEyes.Helpers;
using StarryEyes.Models.Subsystems;
using StarryEyes.Settings;

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
                               var resolved = ParsingExtension.ResolveEntity(s.Text);
                               if (!Setting.NewTextCounting.Value)
                               {
                                   // count with cosidering surrogate pairs
                                   return new StringInfo(resolved).LengthInTextElements;
                               }
                               else
                               {
                                   return GetLength(resolved);
                               }
                           })
                           .Sum();
        }

        public static string AutoEscape(string text)
        {
            return TwitterRegexPatterns.ValidUrl.Replace(text, match =>
            {
                if (String.IsNullOrEmpty(match.Groups[TwitterRegexPatterns.ValidUrlGroupProtocol].Value))
                {
                    // if protocol scheme is not declared, escape url and protect unintended linking.
                    return match.Groups[TwitterRegexPatterns.ValidUrlGroupBefore].Value +
                           EscapeCore(match.Groups[TwitterRegexPatterns.ValidUrlGroupDomain].Value) +
                           match.Groups[TwitterRegexPatterns.ValidUrlGroupPort] +
                           match.Groups[TwitterRegexPatterns.ValidUrlGroupPath] +
                           match.Groups[TwitterRegexPatterns.ValidUrlGroupQueryString];
                }
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

        public static int GetLength(string text)
        {
            var length = 0;
            foreach (var character in text)
            {
                if (char.IsLowSurrogate(character)) continue;
                if (IsHalfWeight(character))
                {
                    length += 1;
                    continue;
                }
                length += 2;
            }
            return length;
        }

        private static bool IsHalfWeight(char character)
        {
            return 0 <= character && character <= 4351 ||
                   8192 <= character && character <= 8205 ||
                   8208 <= character && character <= 8223 ||
                   8242 <= character && character <= 8247;
        }

        /// <summary>
        /// 文字列をトークン化します。
        /// </summary>
        public static IEnumerable<TextToken> Tokenize(string raw)
        {
            if (String.IsNullOrEmpty(raw)) yield break;
            var escaped = ParsingExtension.EscapeEntity(raw);

            // capture URL
            escaped = TwitterRegexPatterns.ValidUrl.Replace(escaped, m =>
                m.Groups[TwitterRegexPatterns.ValidUrlGroupBefore] + "<U>" +
                // # => &sharp; (ハッシュタグで再識別されることを防ぐ)
                m.Groups[TwitterRegexPatterns.ValidUrlGroupUrl].Value
                 .Replace("#", "&sharp;") +
                "<");

            // capture Mention
            escaped = TwitterRegexPatterns.ValidMentionOrList.Replace(escaped, m =>
                m.Groups[TwitterRegexPatterns.ValidMentionOrListGroupBefore].Value +
                "<A>" +
                m.Groups[TwitterRegexPatterns.ValidMentionOrListGroupAt].Value +
                m.Groups[TwitterRegexPatterns.ValidMentionOrListGroupUsername].Value +
                m.Groups[TwitterRegexPatterns.ValidMentionOrListGroupList].Value +
                "<");

            // capture Hashtag
            escaped = TwitterRegexPatterns.ValidHashtag.Replace(escaped, m =>
                m.Groups[TwitterRegexPatterns.ValidHashtagGroupBefore].Value +
                "<H>" +
                m.Groups[TwitterRegexPatterns.ValidHashtagGroupHash].Value +
                m.Groups[TwitterRegexPatterns.ValidHashtagGroupTag].Value +
                "<");

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
                        case 'S':
                            yield return new TextToken(TokenKind.Symbol, body);
                            break;
                        default:
                            throw new InvalidOperationException("invalid grouping:" + kind);
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

        public TokenKind Kind { get; }

        public string Text { get; }
    }

    public enum TokenKind
    {
        Text,
        Url,
        Hashtag,
        AtLink,
        Symbol
    }
}