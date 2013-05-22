using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text.RegularExpressions;

namespace StarryEyes.Octave.Imaging
{
    public static class ImageResolver
    {
        public static readonly Regex UrlRegex = new Regex(@"((?:https?|shttp)://(?:(?:[-_.!~*'()a-zA-Z0-9;:&=+$,]|%[0-9A-Fa-f][0-9A-Fa-f])*@)?(?:(?:[a-zA-Z0-9](?:[-a-zA-Z0-9]*[a-zA-Z0-9])?\.)*[a-zA-Z](?:[-a-zA-Z0-9]*[a-zA-Z0-9])?\.?|[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+)(?::[0-9]*)?(?:/(?:[-_.!~*'()a-zA-Z0-9:@&=+$,]|%[0-9A-Fa-f][0-9A-Fa-f])*(?:;(?:[-_.!~*'()a-zA-Z0-9:@&=+$,]|%[0-9A-Fa-f][0-9A-Fa-f])*)*(?:/(?:[-_.!~*'()a-zA-Z0-9:@&=+$,]|%[0-9A-Fa-f][0-9A-Fa-f])*(?:;(?:[-_.!~*'()a-zA-Z0-9:@&=+$,]|%[0-9A-Fa-f][0-9A-Fa-f])*)*)*)?(?:\?(?:[-_.!~*'()a-zA-Z0-9;/?:@&=+$,]|%[0-9A-Fa-f][0-9A-Fa-f])*)?(?:#(?:[-_.!~*'()a-zA-Z0-9;/?:@&=+$,]|%[0-9A-Fa-f][0-9A-Fa-f])*)?)",
            RegexOptions.Compiled | RegexOptions.Singleline);

        private static readonly Regex FotolifeRegex = new Regex(@"^http://f.hatena.ne.jp/(.*?)/([0-9]{8})(.*?)$", RegexOptions.Compiled | RegexOptions.Singleline);

        private static readonly string[] SupportedExtents = new[] { "png", "gif", "jpg", "jpeg", "bmp" };

        private static readonly Dictionary<string, Func<string, string>> ResolveTable =
            new Dictionary<string, Func<string, string>>
            {
                {"http://twitpic.com/", s => "http://twitpic.com/show/full/" + s.Substring(19)},
                {"http://yfrog.com/", s => s + ":medium"},
                {"http://twitter.yfrog.com/", s => s.Replace("twitter", "") + ":medium"},
                {"http://pckles.com/", s => s + ".png"},
                {"http://pckl.es/", s => s + ".png"},
                {"http://p.twipple.jp", s => "http://p.twipple.jp/show/orig/" + s.Substring(20)},
                {"http://plixi.com/p/", s => "http://api.plixi.com/api/TPAPI.svc/imagefromurl?size=big&url=" + s},
                {"http://tweetphoto.com/", s => "http://api.plixi.com/api/TPAPI.svc/imagefromurl?size=big&url=" + s},
                {"http://lockerz.com/s/", s => "http://api.plixi.com/api/TPAPI.svc/imagefromurl?size=big&url=" + s},
                {"http://movapic.com/pic/", s => "http://image.movapic.com/pic/m_" + s.Substring(23) + ".jpeg"}, // movapic is currently goes bad...
                {"http://movapic.com/", s => "http://image.movapic.com/pic/m_" + s.Substring(19) + ".jpeg"}, // movapic is currently goes bad...
                {"http://f.hatena.ne.jp", s => {
                    var resolve = FotolifeRegex.Match(s);
                    return "http://img.f.hatena.ne.jp/images/fotolife/" + 
                    resolve.Groups[1].Value.Substring(0,1) + "/" + resolve.Groups[1].Value + "/" +
                    resolve.Groups[2].Value + "/" + resolve.Groups[2].Value + resolve.Groups[3].Value + "_120.jpg";
                }},
                {"http://img.ly", s => "http://img.ly/show/full/" + s.Substring(14)},
                {"http://moby.to/", s => s + ":medium"},
                {"http://twitgoo.com/", s => s + "/img"},
                /* {"http://pic.im/", s => "http://pic.im/website/thumbnail/" + s.Substring(14)}, */ // go bad...
                {"http://youtu.be/", s => "http://i.ytimg.com/vi/" + s.Substring(16) + "/default.jpg"},
                {"http://instagr.am/", s => s + "media/?size=m"},
                {"http://photozou.jp/photo/show/", s => "http://photozou.jp/p/img/" + s.Substring(s.LastIndexOf('/') + 1) },
                {"http://p.twipple.jp/", s => "http://p.twipple.jp/show/large/" + s.Substring(20)},
                {"http://p.twimg.com/", s => s},
                {"http://via.me/", s => s + "/thumb/r600x600"},
                {"http://www.pixiv.net/member_illust.php?", p =>
                {
                    var builder = new UriBuilder(p) {Scheme = "pixiv"};
                    return builder.Uri.OriginalString;
                }}
            };

        public static IObservable<Tuple<Uri, Uri>> Resolve(string text)
        {
            return Observable.Start(() => UrlRegex.Matches(text).Cast<Match>())
                .SelectMany(_ => _)
                .Select(s => s.Value)
                .Select(s => new { key = ResolveTable.Keys.FirstOrDefault(s.StartsWith), value = s })
                .Select(t =>
                {
                    try
                    {
                        if (SupportedExtents.Any(ext => t.value.EndsWith("." + ext)))
                            return new { original = t.value, resolved = t.value };
                        if (t.key != null)
                            return new { original = t.value, resolved = ResolveTable[t.key](t.value) };
                    }
                    catch
                    {
                    }
                    return null;
                })
                .Where(t => t != null && t.resolved != null && Uri.IsWellFormedUriString(t.resolved, UriKind.Absolute))
                .Select(u => Tuple.Create(new Uri(u.original), new Uri(u.resolved)));
        }
    }
}
