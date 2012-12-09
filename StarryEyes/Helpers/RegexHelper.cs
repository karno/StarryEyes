using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace StarryEyes
{
    public static class RegexHelper
    {
        #region TLDs

        private static string[] TLD = new[]{"com", "net", "edu", "gov", "mil", "org", "info",
            "biz", "name", "pro", "aero", "coop", "museum", "jobs", "travel", "mail", "cat",
            "post", "asia", "mobi", "tel", "xxx", "int"};
        private static string[] ccTLD = new[]
        {
            // A
            "ac","ad","ae","af","ag","ai","al","am","an","ao","aq","ar","as","at","au","aw","ax","az",
            // B
            "ba","bb","bd","be","bf","bg","bh","bi","bj","bl","bm","bn","bo","br","bs","bt","bu","bv","bw","by","bz",
            // C
            "ca","cc","cd","cf","cg","ch","ci","ck","cl","cm","cn","co","cr","cs","cu","cv","cx","cy","cz",
            // D
            "dd","de","dg","dj","dk","dm","do","dz",
            // E
            "ec","ee","eg","eh","er","es","et","eu",
            // F
            "fi","fj","fk","fm","fo","fr",
            // G
            "ga","gb","gd","ge","gf","gg","gh","gi","gl","gm","gn","gp","gq","gr","gs","gt","gu","gw","gy",
            // H
            "hk","hm","hn","hr","ht","hu",
            // I
            "id","ie","il","im","in","io","iq","ir","is","it",
            // J
            "je","jm","jo","jp",
            // K
            "ke","kg","kh","ki","km","kn","kp","kr","kw","ky","kz",
            // L
            "la","lb","lc","li","lk","lr","ls","lt","lu","lv","ly",
            // M
            "ma","mc","md","me","mg","mh","mk","ml","mm","mn","mo","mp","mq","mr","ms","mt","mu","mv","mw","mx","my","mz",
            // N
            "na","nc","ne","nf","ng","ni","nl","no","np","nr","nu","nz",
            // O
            "om",
            // P
            "pa","pe","pf","pg","ph","pk","pl","pm","pn","pr","ps","pt","pw","py",
            // Q
            "qa",
            // R
            "re","ro","rs","ru","rw",
            // S
            "sa","sb","sc","sd","se","sg","sh","si","sj","sk","sl","sm","sn","so","sr","ss","st","su","sv","sy","sz",
            // T
            "tc","td","tf","tg","th","tj","tk","tl","tm","tn","to","tp","tr","tt","tv","tw","tz",
            // U
            "ua","ug","uk","um","us","uy","uz"
            // V
            ,"va","vc","ve","vg","vi","vn","vu",
            // W
            "wf","ws",
            // Y
            "ye","yt","yu",
            // Z
            "za","zm","zw"
        };

        #endregion

        /// <summary>
        /// for @userid
        /// </summary>
        public static Regex AtRegex = new Regex(
            @"(?<![A-Za-z0-9_])@([A-Za-z0-9_]+(?:/[A-Za-z0-9_]*)?)(?![A-Za-z0-9_@])",
            RegexOptions.Compiled | RegexOptions.Singleline);

        /// <summary>
        /// for URL(well-formed)
        /// </summary>
        // Regex from http://www.din.or.jp/~ohzaki/perl.htm#URI
        public static Regex UrlRegex = new Regex(
            @"((?:https?|shttp)://(?:(?:[-_.!~*'()a-zA-Z0-9;:&=+$,]|%[0-9A-Fa-f][0-9A-Fa-f])*@)?(?:(?:[a-zA-Z0-9](?:[-a-zA-Z0-9]*[a-zA-Z0-9])?\.)*[a-zA-Z](?:[-a-zA-Z0-9]*[a-zA-Z0-9])?\.?|[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+)(?::[0-9]*)?(?:/(?:[-_.!~*'()a-zA-Z0-9:@&=+$,]|%[0-9A-Fa-f][0-9A-Fa-f])*(?:;(?:[-_.!~*'()a-zA-Z0-9:@&=+$,]|%[0-9A-Fa-f][0-9A-Fa-f])*)*(?:/(?:[-_.!~*'()a-zA-Z0-9:@&=+$,]|%[0-9A-Fa-f][0-9A-Fa-f])*(?:;(?:[-_.!~*'()a-zA-Z0-9:@&=+$,]|%[0-9A-Fa-f][0-9A-Fa-f])*)*)*)?(?:\?(?:[-_.!~*'()a-zA-Z0-9;/?:@&=+$,]|%[0-9A-Fa-f][0-9A-Fa-f])*)?(?:#(?:[-_.!~*'()a-zA-Z0-9;/?:@&=+$,]|%[0-9A-Fa-f][0-9A-Fa-f])*)?)",
            RegexOptions.Compiled | RegexOptions.Singleline);

        /// <summary>
        /// This is for the URL which is not the URL but be treated as the URL by Fucking Twitter.
        /// </summary>
        public static Regex TwitterUrlTLDRegex = new Regex(
            "([-a-z0-9_\\.]+)(\\.((" + TLD.JoinString("|") + ")|([a-z0-9]+\\.(" + ccTLD.JoinString("|") + ")))" +
            "[-a-z0-9\\/_\\.!~\\*'\\(\\)%]*)", RegexOptions.Singleline | RegexOptions.IgnoreCase);

        /// <summary>
        /// ハッシュタグ用のregex
        /// </summary>
        public static Regex HashRegex = new Regex(@"(?<!\w)([#＃]\w+)",
            RegexOptions.Compiled | RegexOptions.Singleline);

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
