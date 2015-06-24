using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using StarryEyes.Anomaly.Ext;
using StarryEyes.Anomaly.TwitterApi.DataModels;

namespace StarryEyes.Anomaly.TwitterApi.Internals
{
    internal static class ResultHandlers
    {
        public static async Task<string> ReadAsStringAsync(this HttpResponseMessage response)
        {
            return await response.EnsureSuccessStatusCode().Content
                                 .ReadAsStringAsync().ConfigureAwait(false);
        }


        public static Task<TwitterUser> ReadAsUserAsync(HttpResponseMessage response)
        {
            return ReadAsAsync(response, d => new TwitterUser(d));
        }

        public static Task<TwitterStatus> ReadAsStatusAsync(HttpResponseMessage response)
        {
            return ReadAsAsync(response, d => new TwitterStatus(d));
        }

        public static Task<TwitterList> ReadAsListAsync(HttpResponseMessage response)
        {
            return ReadAsAsync(response, d => new TwitterList(d));
        }

        public static Task<TwitterConfiguration> ReadAsConfigurationAsync(HttpResponseMessage response)
        {
            return ReadAsAsync(response, d => new TwitterConfiguration(d));
        }

        public static Task<TwitterSavedSearch> ReadAsSavedSearchAsync(HttpResponseMessage response)
        {
            return ReadAsAsync(response, d => new TwitterSavedSearch(d));
        }

        public static Task<TwitterFriendship> ReadAsFriendshipAsync(HttpResponseMessage response)
        {
            return ReadAsAsync(response, d => new TwitterFriendship(d));
        }

        private static async Task<T> ReadAsAsync<T>(
            HttpResponseMessage response, Func<dynamic, T> instantiator)
        {
            var json = await response.ReadAsStringAsync().ConfigureAwait(false);
            return instantiator(DynamicJson.Parse(json));
        }



        public static Task<IEnumerable<TwitterUser>> ReadAsUserCollectionAsync(HttpResponseMessage response)
        {
            return ReadAsCollectionAsync(response, u => new TwitterUser(u));
        }

        public static Task<IEnumerable<TwitterList>> ReadAsListCollectionAsync(HttpResponseMessage response)
        {
            return ReadAsCollectionAsync(response, l => new TwitterList(l));
        }

        public static Task<IEnumerable<long>> ReadAsIdCollectionAsync(HttpResponseMessage response)
        {
            return ReadAsCollectionAsync(response, d => (long)d);
        }

        public static Task<IEnumerable<TwitterSavedSearch>> ReadAsSavedSearchCollectionAsync(HttpResponseMessage response)
        {
            return ReadAsCollectionAsync(response, d => new TwitterSavedSearch(d));
        }

        public static async Task<IEnumerable<TwitterStatus>> ReadAsStatusCollectionAsync(HttpResponseMessage response)
        {
            return await Task.Run(async () =>
            {
                var json = await response.ReadAsStringAsync().ConfigureAwait(false);
                var parsed = DynamicJson.Parse(json);
                if (parsed.statuses())
                {
                    parsed = parsed.statuses;
                }
                return ((dynamic[])parsed).Select(status => new TwitterStatus(status));
            }).ConfigureAwait(false);
        }

        private static async Task<IEnumerable<T>> ReadAsCollectionAsync<T>(
            HttpResponseMessage response, Func<dynamic, T> factory)
        {
            var json = await response.ReadAsStringAsync().ConfigureAwait(false);
            return await Task.Run(() => (((dynamic[])DynamicJson.Parse(json))
                .Select(list => (T)factory(list)))).ConfigureAwait(false);
        }



        public static Task<ICursorResult<IEnumerable<long>>> ReadAsCursoredIdsAsync(HttpResponseMessage response)
        {
            return ReadAsCursoredAsync(response, json => json.ids, d => (long)d);
        }

        public static Task<ICursorResult<IEnumerable<TwitterUser>>> ReadAsCursoredUsersAsync(HttpResponseMessage response)
        {
            return ReadAsCursoredAsync(response, json => json.users, d => new TwitterUser(d));
        }

        private static async Task<ICursorResult<IEnumerable<T>>> ReadAsCursoredAsync<T>(
                    HttpResponseMessage response, Func<dynamic, dynamic> selector, Func<dynamic, T> instantiator)
        {
            return await Task.Run(async () =>
            {
                var json = await response.ReadAsStringAsync().ConfigureAwait(false);
                var parsed = DynamicJson.Parse(json);
                var converteds = ((dynamic[])selector(parsed)).Select(d => (T)instantiator(d));
                return new CursorResult<IEnumerable<T>>(converteds,
                    parsed.previous_cursor_str, parsed.next_cursor_str);
            }).ConfigureAwait(false);
        }

    }
}
