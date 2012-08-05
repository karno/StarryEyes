using System;
using System.Collections.Generic;
using System.Net;
using System.Reactive.Linq;

namespace StarryEyes.Mystique.Scrapping
{
    public class InstapaperApi : ScrappingService
    {
        const string InstapaperApiEndpoint = "https://www.instapaper.com/api/";

        public InstapaperApi(string userId, string password) : base(userId, password) { }

        public override IObservable<bool> CheckAuth()
        {
            var param = new Dictionary<string, string>()
            {
                {"username", this.UserId},
                {"password", this.Password},
            };
            return SendRequest(InstapaperApiEndpoint + "authenticate", param)
                .Select(s => s.StatusCode == HttpStatusCode.OK);
        }

        public override IObservable<bool> Scrap(string url, string title = null, long? sourceTweetId = null)
        {
            var param = new Dictionary<string, string>()
            {
                {"username", this.UserId},
                {"password", this.Password},
                {"url", url},
                {"title", title},
            };
            return SendRequest(InstapaperApiEndpoint + "add", param)
                .Select(s => s.StatusCode == HttpStatusCode.OK)
                .Catch((Exception excp) =>
                {
                    if (excp.ToString().Contains("WebHeaderInvalidControlChars"))
                    {
                        // F**KING M$
                        return Observable.Return(true);
                    }
                    else
                    {
                        return Observable.Throw<bool>(excp);
                    }
                });
        }
    }
}

