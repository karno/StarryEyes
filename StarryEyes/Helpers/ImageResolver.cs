using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Cadena.Data;
using Cadena.Data.Entities;

namespace StarryEyes.Helpers
{
    public static class ImageResolver
    {
        public static readonly Regex UrlRegex =
            new Regex(
                @"((?:https?|shttp)://(?:(?:[-_.!~*'()a-zA-Z0-9;:&=+$,]|%[0-9A-Fa-f][0-9A-Fa-f])*@)?(?:(?:[a-zA-Z0-9](?:[-a-zA-Z0-9]*[a-zA-Z0-9])?\.)*[a-zA-Z](?:[-a-zA-Z0-9]*[a-zA-Z0-9])?\.?|[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+)(?::[0-9]*)?(?:/(?:[-_.!~*'()a-zA-Z0-9:@&=+$,]|%[0-9A-Fa-f][0-9A-Fa-f])*(?:;(?:[-_.!~*'()a-zA-Z0-9:@&=+$,]|%[0-9A-Fa-f][0-9A-Fa-f])*)*(?:/(?:[-_.!~*'()a-zA-Z0-9:@&=+$,]|%[0-9A-Fa-f][0-9A-Fa-f])*(?:;(?:[-_.!~*'()a-zA-Z0-9:@&=+$,]|%[0-9A-Fa-f][0-9A-Fa-f])*)*)*)?(?:\?(?:[-_.!~*'()a-zA-Z0-9;/?:@&=+$,]|%[0-9A-Fa-f][0-9A-Fa-f])*)?(?:#(?:[-_.!~*'()a-zA-Z0-9;/?:@&=+$,]|%[0-9A-Fa-f][0-9A-Fa-f])*)?)",
                RegexOptions.Compiled | RegexOptions.Singleline);

        private static readonly Regex FotolifeRegex = new Regex(@"^http://f.hatena.ne.jp/(.*?)/([0-9]{8})(.*?)$",
            RegexOptions.Compiled | RegexOptions.Singleline);

        private static readonly string[] SupportedExtents = new[] { "png", "gif", "jpg", "jpeg", "bmp" };

        private static readonly Dictionary<string, Func<string, string>> ResolveTable =
            new Dictionary<string, Func<string, string>>
            {
                { "http://totori.dip.jp", s => "http://totori.dip.jp/totori_vita.jpg" },
                { "http://twitpic.com/d250g2", s => "http://d250g2.com/d250g2.jpg" },
                { "http://d250g2.com", s => "http://d250g2.com/d250g2.jpg" },
                {
                    "http://twitpic.com/",
                    s => PreCheck(s.Length > 19, () => "http://twitpic.com/show/full/" + s.Substring(19))
                },
                { "http://yfrog.com/", s => s + ":medium" },
                { "http://twitter.yfrog.com/", s => s.Replace("twitter", "") + ":medium" },
                { "http://pckles.com/", s => s + ".png" },
                { "http://pckl.es/", s => s + ".png" },
                { "http://gyazo.com/", s => s + ".png" },
                {
                    "http://p.twipple.jp",
                    s => PreCheck(s.Length > 20, () => "http://p.twipple.jp/show/orig/" + s.Substring(20))
                },
                { "http://plixi.com/p/", s => "http://api.plixi.com/api/TPAPI.svc/imagefromurl?size=big&url=" + s },
                { "http://tweetphoto.com/", s => "http://api.plixi.com/api/TPAPI.svc/imagefromurl?size=big&url=" + s },
                { "http://lockerz.com/s/", s => "http://api.plixi.com/api/TPAPI.svc/imagefromurl?size=big&url=" + s },
                {
                    "http://movapic.com/pic/",
                    s => PreCheck(s.Length > 23, () => "http://image.movapic.com/pic/m_" + s.Substring(23) + ".jpeg")
                },
                // movapic is currently goes bad...
                {
                    "http://movapic.com/",
                    s => PreCheck(s.Length > 19, () => "http://image.movapic.com/pic/m_" + s.Substring(19) + ".jpeg")
                },
                // movapic is currently goes bad...
                {
                    "http://f.hatena.ne.jp", s =>
                    {
                        var resolve = FotolifeRegex.Match(s);
                        if (resolve.Groups[1].Value.Length == 0) return null;
                        return "http://img.f.hatena.ne.jp/images/fotolife/" +
                               resolve.Groups[1].Value.Substring(0, 1) + "/" + resolve.Groups[1].Value + "/" +
                               resolve.Groups[2].Value + "/" + resolve.Groups[2].Value + resolve.Groups[3].Value +
                               "_120.jpg";
                    }
                },
                { "http://img.ly", s => PreCheck(s.Length > 14, () => "http://img.ly/show/full/" + s.Substring(14)) },
                { "http://moby.to/", s => s + ":medium" },
                { "http://twitgoo.com/", s => s + "/img" },
                /* {"http://pic.im/", s => "http://pic.im/website/thumbnail/" + s.Substring(14)}, */ // go bad...
                {
                    "http://youtu.be/",
                    s => PreCheck(s.Length > 16, () => "http://i.ytimg.com/vi/" + s.Substring(16) + "/default.jpg")
                },
                { "http://instagr.am/p/", s => EnsureEndsWithSlash(s) + "media/?size=m" },
                { "http://instagram.com/p/", s => EnsureEndsWithSlash(s) + "media/?size=m" },
                {
                    "http://photozou.jp/photo/show/",
                    s =>
                    {
                        var index = s.LastIndexOf('/') + 1;
                        if (s.Length <= index) return null;
                        return "http://photozou.jp/p/img/" + s.Substring(index);
                    }
                },
                {
                    "http://p.twipple.jp/",
                    s => PreCheck(s.Length > 20, () => "http://p.twipple.jp/show/large/" + s.Substring(20))
                },
                { "http://p.twimg.com/", s => s },
                {
                    "http://www.pixiv.net/member_illust.php?", p =>
                    {
                        // for deferred resolver
                        var builder = new UriBuilder(p) { Scheme = "pixiv" };
                        return builder.Uri.OriginalString;
                    }
                }
            };

        private static string EnsureEndsWithSlash(string url)
        {
            return url.EndsWith("/") ? url : url + "/";
        }

        private static string PreCheck(bool condition, Func<string> stringProvider)
        {
            return condition ? stringProvider() : null;
        }

        /// <summary>
        /// Get attached images in status. <para />
        /// Returns tuples of (original_uri, image_uri).
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        public static IEnumerable<Tuple<Uri, Uri>> ResolveImages(TwitterStatus status)
        {
            var result = new List<Tuple<string, string>>();

            // pick attached images 
            status.Entities.Guard()
                  .OfType<TwitterMediaEntity>()
                  .ForEach(e => result.Add(Tuple.Create(e.Url, e.MediaUrl)));

            // resolve url in status text
            var matches = UrlRegex.Matches(status.GetEntityAidedText(EntityDisplayMode.LinkUri)).Cast<Match>();
            matches.Select(m => m.Value).ForEach(s =>
            {
                if (SupportedExtents.Any(ext => s.EndsWith("." + ext)))
                {
                    result.Add(Tuple.Create(s, s));
                }
                else
                {
                    var key = ResolveTable.Keys.FirstOrDefault(s.StartsWith);
                    Func<string, string> resolver;
                    if (key != null && ResolveTable.TryGetValue(key, out resolver))
                    {
                        result.Add(Tuple.Create(s, resolver(s)));
                    }
                }
            });

            return result.Distinct(t => t.Item2)
                         .Where(t => Uri.IsWellFormedUriString(t.Item1, UriKind.Absolute) &&
                                     Uri.IsWellFormedUriString(t.Item2, UriKind.Absolute))
                         .Select(t => Tuple.Create(new Uri(t.Item1), new Uri(t.Item2)));
        }
    }
}