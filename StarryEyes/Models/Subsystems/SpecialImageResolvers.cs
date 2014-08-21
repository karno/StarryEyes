using System;
using System.Net;
using System.Text.RegularExpressions;
using StarryEyes.Views.Controls;

namespace StarryEyes.Models.Subsystems
{
    public static class SpecialImageResolvers
    {
        private static readonly Regex PixivRegex = new Regex("(http://[^\"]+?\\.pixiv\\.net/[^\"]+?_m\\.jpg)",
                                                             RegexOptions.Compiled | RegexOptions.Singleline);
        public static void Initialize()
        {
            ImageLoader.RegisterSpecialResolverTable("pixiv", uri =>
            {
                try
                {
                    var builder = new UriBuilder(uri) { Scheme = "http" };
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
                catch (Exception)
                {
                }
                return new byte[0];
            });
        }

        #region Extended WebClient
        private sealed class WebClientEx : WebClient
        {
            public CookieContainer CookieContainer { get; set; }

            public string Referer { get; set; }

            protected override WebRequest GetWebRequest(Uri uri)
            {
                var req = base.GetWebRequest(uri);

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
        #endregion
    }
}
