using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using StarryEyes.Breezy.Util;

namespace StarryEyes.Scrapping
{
    public abstract class ScrappingService
    {
        protected string UserId { get; private set; }
        protected string Password { get; private set; }

        public ScrappingService(string userId, string password)
        {
            this.UserId = userId;
            this.Password = password;
        }

        public abstract IObservable<bool> CheckAuth();

        public abstract IObservable<bool> Scrap(string url, string title = null, long? sourceTweetId = null);

        protected IObservable<HttpWebResponse> SendRequest(string url, Dictionary<string, string> parameters, Action<HttpWebRequest> attach = null)
        {
            var param = parameters
                .Where(p => p.Value != null)
                .Select(p => p.Key + "=" + HttpUtility.UrlEncode(p.Value))
                .JoinString("&");
            var req = HttpWebRequest.Create(url + "?" + param) as HttpWebRequest;
            if (attach != null)
                attach(req);
            return Observable.FromAsyncPattern<WebResponse>(req.BeginGetResponse, req.EndGetResponse)()
                .Select(w => (HttpWebResponse)w);
        }
    }
}
