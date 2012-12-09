using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Text;
using Newtonsoft.Json;
using StarryEyes.Breezy.Api.Parsing.JsonFormats;
using StarryEyes.Breezy.Authorize;
using StarryEyes.Breezy.DataModel;
using StarryEyes.Breezy.Util;

namespace StarryEyes.Breezy.Api.Parsing
{
    public static class Parser
    {
        #region updating rate-limit informations

        public static IObservable<WebResponse> UpdateRateLimitInfo(
            this IObservable<WebResponse> resp, AuthenticateInfo authenticator)
        {
            return resp;
            /*
            return resp.Do(r =>
            {
                int rl;
                if (int.TryParse(r.Headers["X-RateLimit-Limit"], out rl))
                    authenticator.RateLimitMax = rl;
                int rlc;
                if (int.TryParse(r.Headers["X-RateLimit-Remaining"], out rlc))
                    authenticator.RateLimitRemaining = rlc;
                long rlr;
                if (long.TryParse(r.Headers["X-RateLimit-Reset"], out rlr))
                    authenticator.RateLimitReset = UnixEpoch.GetDateTimeByUnixEpoch(rlr);
            });
            */
        }

        #endregion

        public static IObservable<T> StreamRead<T>(this IObservable<string> downloaded, Func<Stream, T> converter)
        {
            return downloaded.Select(s =>
            {
                try
                {
                    return new MemoryStream(Encoding.UTF8.GetBytes(s)).Using(ms => converter(ms));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex + Environment.NewLine + "Corrupt:: " + Environment.NewLine + s);
                    return default(T);
                }
            });
        }

        public static IObservable<T> DeserializeJson<T>(this IObservable<string> downloaded)
        {
            return downloaded.StreamRead(s => new StreamReader(s)
                .Using(sr => new JsonTextReader(sr)
                    .Using(jtr => new JsonSerializer().Deserialize<T>(jtr))));
        }

        public static IObservable<List<T>> DeserializeJsonArray<T>(this IObservable<string> downloaded)
        {
            return downloaded.StreamRead(s => new StreamReader(s)
                .Using(sr => new JsonTextReader(sr)
                    .Using(jtr => new JsonSerializer().Deserialize<List<T>>(jtr))));
        }

        public static IObservable<string> ReadString(this IObservable<WebResponse> webres)
        {
            return webres
                .SelectMany(r => r.DownloadStringAsync())
                .Where(s => !String.IsNullOrEmpty(s));
        }

        public static IObservable<string> ReadStringLines(this IObservable<WebResponse> webres)
        {
            return webres
                .SelectMany(r => r.DownloadStringLineAsync())
                .Where(s => !String.IsNullOrEmpty(s));
        }

        #region Specialized parsers

        public static IObservable<TwitterStatus> ReadTimeline(this IObservable<WebResponse> response)
        {
            return response.ReadString()
                .DeserializeJsonArray<TweetJson>()
                .Where(_ => _ != null)
                .Select(_ => _.Select(__ => __.Spawn()).OrderBy(__ => __.CreatedAt))
                .SelectMany(_ => _);
        }

        public static IObservable<TwitterStatus> ReadTweet(this IObservable<WebResponse> response)
        {
            return response
                .ReadString()
                .DeserializeJson<TweetJson>()
                .Where(_ => _ != null)
                .Select(_ => _.Spawn());
        }

        public static IObservable<TwitterStatus> ReadDirectMessages(this IObservable<WebResponse> response)
        {
            return response.ReadString()
                .DeserializeJsonArray<DirectMessageJson>()
                .Where(_ => _ != null)
                .Select(_ => _.Select(__ => __.Spawn()).OrderBy(__ => __.CreatedAt))
                .SelectMany(_ => _);
        }

        public static IObservable<TwitterStatus> ReadDirectMessage(this IObservable<WebResponse> response)
        {
            return response.ReadString()
                .DeserializeJson<DirectMessageJson>()
                .Where(_ => _ != null)
                .Select(_ => _.Spawn());
        }

        public static IObservable<TwitterUser> ReadUsers(this IObservable<WebResponse> response)
        {
            return response.ReadString()
                .DeserializeJsonArray<UserJson>()
                .Where(_ => _ != null)
                .SelectMany(_ => _)
                .Select(_ => _.Spawn());
        }

        public static IObservable<TwitterUser> ReadUser(this IObservable<WebResponse> response)
        {
            return response.ReadString()
                .DeserializeJson<UserJson>()
                .Where(_ => _ != null)
                .Select(_ => _.Spawn());
        }

        public static IObservable<TwitterList> ReadLists(this IObservable<WebResponse> response)
        {
            return response.ReadString()
                .DeserializeJsonArray<ListJson>()
                .Where(_ => _ != null)
                .SelectMany(_ => _)
                .Select(_ => _.Spawn());
        }

        public static IObservable<TwitterList> ReadList(this IObservable<WebResponse> response)
        {
            return response.ReadString()
                .DeserializeJson<ListJson>()
                .Where(_ => _ != null)
                .Select(_ => _.Spawn());
        }

        public static IObservable<long> ReadIdsArray(this IObservable<WebResponse> response)
        {
            return response.ReadString()
                .DeserializeJsonArray<long>()
                .Where(_ => _ != null)
                .SelectMany(_ => _);
        }

        #endregion
    }
}
