using System;
using System.Collections.Generic;
using System.Net;
using System.Reactive.Linq;

namespace StarryEyes.Mystique.Scrapping
{
    public class PocketApi : ScrappingService
    {
        const string PocketApiEndpoint = "https://readitlaterlist.com/v2/";

        private string _apikey;
        public PocketApi(string apiKey, string userId, string password)
            : base(userId, password)
        {
            this._apikey = apiKey;
        }

        public override IObservable<bool> CheckAuth()
        {
            var param = new Dictionary<string, string>()
            {
                {"apikey", _apikey},
                {"username", UserId},
                {"password", Password},
            };
            return SendRequest(PocketApiEndpoint + "auth", param)
                .Select(r => r.StatusCode == HttpStatusCode.OK);
        }

        public override IObservable<bool> Scrap(string url, string title = null, long? sourceTweetId = null)
        {
            var param = new Dictionary<string, string>()
            {
                {"apikey", _apikey},
                {"username", UserId},
                {"password", Password},
                {"url", url},
                {"title", title},
                {"ref_id", sourceTweetId.HasValue ? sourceTweetId.Value.ToString() : null},
            };
            return SendRequest(PocketApiEndpoint + "add", param)
                .Select(r => r.StatusCode == HttpStatusCode.OK);
        }
    }
}
