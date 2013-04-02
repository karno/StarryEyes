using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace StarryEyes
{
    public static class RegexHelper
    {
        /// <summary>
        /// ダイレクトメッセージ送信判定用のregex
        /// </summary>
        public static Regex DirectMessageSendRegex = new Regex(
            @"^d (?<![A-Za-z0-9_])@([A-Za-z0-9_]+(?:/[A-Za-z0-9_]*)?)(?![A-Za-z0-9_@]) (.*)$");

        /// <summary>
        /// Tokenize by regex.<para />
        /// if source text contains &lt; or &gt; this is automatically replaced. so, 
        /// some regex is not work well.
        /// </summary>
        /// <param name="regex">matching regex.</param>
        /// <param name="source">text source</param>
        /// <returns>separated strings</returns>
        public static IEnumerable<Tuple<string, bool>> Tokenize(this Regex regex, string source)
        {
            var separated = regex.Replace(
                EscapeInnerEntity(source),
                target =>
                {
                    return "<" + target + ">";
                });
            return separated.Split(new[] { '>' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Split('<'))
                .SelectMany(s =>
                s.Length == 1 ?
                new[] { Tuple.Create(UnescapeInnerEntity(s[0]), false) } :
                new[] 
                {
                    Tuple.Create(UnescapeInnerEntity(s[0]), false),
                    Tuple.Create(UnescapeInnerEntity(s[1]), true)
                })
                .Where(t => !String.IsNullOrEmpty(t.Item1));
        }

        private static string EscapeInnerEntity(string unescaped)
        {
            return unescaped.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
        }

        private static string UnescapeInnerEntity(string escaped)
        {
            return escaped.Replace("&lt;", "<").Replace("&gt;", ">").Replace("&amp;", "&");
        }
    }
}
