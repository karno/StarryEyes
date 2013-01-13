using System;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using StarryEyes.Views.Controls;

namespace StarryEyes.Models.Subsystems
{
    public static class SpecialImageResolverRegister
    {
        private static readonly Regex PixivRegex = new Regex("(http://[^\"]+?\\.pixiv\\.net/[^\"]+?_m\\.jpg)", RegexOptions.Compiled | RegexOptions.Singleline);
        public static void Initialize()
        {
            LazyImage.RegisterSpecialResolverTable("pixiv", uri =>
            {
                try
                {
                    var builder = new UriBuilder(uri);
                    builder.Scheme = "http";
                    uri = builder.Uri;
                    using (var wc = new WebClientEx())
                    {
                        wc.CookieContainer = new CookieContainer();

                        var src = wc.DownloadString(uri);
                        var match = PixivRegex.Match(src);
                        if (match.Success)
                        {
                            wc.Referer = uri.OriginalString;
                            return wc.DownloadData(match.Groups[1].Value);
                        }
                    }
                }
                catch (WebException)
                {
                }
                catch (SocketException)
                {
                }
                return new byte[0];
            });
        }
    }

    class WebClientEx : System.Net.WebClient
    {
        public CookieContainer CookieContainer { get; set; }

        public string Referer { get; set; }

        protected override WebRequest GetWebRequest(Uri uri)
        {
            WebRequest req = base.GetWebRequest(uri);

            var hwr = req as HttpWebRequest;
            if (hwr != null)
            {
                hwr.CookieContainer = this.CookieContainer;
                hwr.UserAgent = "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.0; Trident/5.0)";
                hwr.Referer = this.Referer;
            }
            return req;
        }
    }
}
