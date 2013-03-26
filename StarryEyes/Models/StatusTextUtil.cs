using System;
using System.Collections.Generic;
using System.Linq;

namespace StarryEyes.Models
{
    public static class StatusTextUtil
    {
        public const int MaxTextLength = 140;

        public const int HttpUrlLength = 20; // http
        public const int HttpsUrlLength = HttpUrlLength + 1; // https

        public static int CountText(string text)
        {
            return RegexHelper.UrlRegex.Tokenize(text.Replace("\r\n", "\n"))
                .SelectMany(s =>
                {
                    if (s.Item2) // URL matched
                        return new[] { s };
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
                    return Tuple.Create(
                        RegexHelper.TwitterUrlTLDRegex.Replace(s.Item1,
                                                               match => match.Groups[1].Value + " " + match.Groups[2].Value
                            ), true);
                })
                .Select(s => s.Item1)
                .JoinString("");
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
            escaped = RegexHelper.UrlRegex.Replace(escaped, m =>
            {
                // # => &sharp; (ハッシュタグで再識別されることを防ぐ)
                var repl = m.Groups[1].Value.Replace("#", "&sharp;");
                return "<U>" + repl + "<";
            });
            escaped = RegexHelper.AtRegex.Replace(escaped, "<A>@$1<");
            escaped = RegexHelper.HashRegex.Replace(escaped, m =>
            {
                if (m.Groups.Count > 0)
                {
                    return "<H>" + m.Groups[0].Value + "<";
                }
                return m.Value;
            });
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
