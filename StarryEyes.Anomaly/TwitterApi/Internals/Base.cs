using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.Utils;

namespace StarryEyes.Anomaly.TwitterApi.Internals
{
    internal static class Base
    {
        public static async Task<IApiResult<T>> PostAsync<T>([NotNull] this IOAuthCredential credential,
           [NotNull] IApiAccessProperties properties, [NotNull] string path,
           [NotNull] HttpContent content, [NotNull] Func<HttpResponseMessage, Task<T>> converter,
           CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (properties == null) throw new ArgumentNullException("properties");
            if (path == null) throw new ArgumentNullException("path");
            if (content == null) throw new ArgumentNullException("content");
            if (converter == null) throw new ArgumentNullException("converter");
            using (var client = credential.CreateOAuthClient())
            using (var resp = await client.PostAsync(properties, path, content,
                cancellationToken).ConfigureAwait(false))
            {
                return await converter(resp).ToApiResult(resp).ConfigureAwait(false);
            }
        }

        public static async Task<IApiResult<T>> PostAsync<T>([NotNull] this IOAuthCredential credential,
           [NotNull] IApiAccessProperties properties, [NotNull] string path,
           [NotNull] Dictionary<string, object> parameter,
           [NotNull] Func<HttpResponseMessage, Task<T>> converter,
           CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (properties == null) throw new ArgumentNullException("properties");
            if (path == null) throw new ArgumentNullException("path");
            if (parameter == null) throw new ArgumentNullException("parameter");
            if (converter == null) throw new ArgumentNullException("converter");
            using (var client = credential.CreateOAuthClient())
            using (var resp = await client.PostAsync(properties, path, parameter,
                cancellationToken).ConfigureAwait(false))
            {
                return await converter(resp).ToApiResult(resp).ConfigureAwait(false);
            }
        }

        public static async Task<IApiResult<T>> GetAsync<T>([NotNull] this IOAuthCredential credential,
           [NotNull] IApiAccessProperties properties, [NotNull] string path,
           [NotNull] Dictionary<string, object> parameter,
           [NotNull] Func<HttpResponseMessage, Task<T>> converter,
           CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (properties == null) throw new ArgumentNullException("properties");
            if (path == null) throw new ArgumentNullException("path");
            if (parameter == null) throw new ArgumentNullException("parameter");
            using (var client = credential.CreateOAuthClient())
            using (var resp = await client.GetAsync(properties, path, parameter,
                cancellationToken).ConfigureAwait(false))
            {
                return await converter(resp).ToApiResult(resp).ConfigureAwait(false);
            }
        }

        private static async Task<IApiResult<T>> ToApiResult<T>([NotNull] this Task<T> result,
            [NotNull] HttpResponseMessage msg)
        {
            if (result == null) throw new ArgumentNullException("result");
            if (msg == null) throw new ArgumentNullException("msg");
            return ApiResult.Create(await result.ConfigureAwait(false), msg);
        }

        private static Task<HttpResponseMessage> GetAsync([NotNull] this HttpClient client,
           [NotNull] IApiAccessProperties properties, [NotNull] string path,
           [NotNull] Dictionary<string, object> parameter, CancellationToken cancellationToken)
        {
            if (client == null) throw new ArgumentNullException("client");
            if (properties == null) throw new ArgumentNullException("properties");
            if (path == null) throw new ArgumentNullException("path");
            if (parameter == null) throw new ArgumentNullException("parameter");
            return client.GetAsync(FormatUrl(properties.Endpoint, path, parameter.ParametalizeForGet()),
                cancellationToken);
        }

        private static Task<HttpResponseMessage> PostAsync([NotNull] this HttpClient client,
           [NotNull] IApiAccessProperties properties,
           string path, [NotNull] Dictionary<string, object> parameter, CancellationToken cancellationToken)
        {
            if (client == null) throw new ArgumentNullException("client");
            if (properties == null) throw new ArgumentNullException("properties");
            if (parameter == null) throw new ArgumentNullException("parameter");
            return client.PostAsync(properties, path,
                parameter.ParametalizeForPost(), cancellationToken);
        }

        private static Task<HttpResponseMessage> PostAsync([NotNull] this HttpClient client,
           [NotNull] IApiAccessProperties properties, [NotNull] string path, [NotNull] HttpContent content,
           CancellationToken cancellationToken)
        {
            if (client == null) throw new ArgumentNullException("client");
            if (properties == null) throw new ArgumentNullException("properties");
            if (path == null) throw new ArgumentNullException("path");
            if (content == null) throw new ArgumentNullException("content");
            return client.PostAsync(FormatUrl(properties.Endpoint, path),
                content, cancellationToken);
        }


        private static string FormatUrl(string endpoint, string path)
        {
            return HttpUtility.ConcatUrl(endpoint, path);
        }

        private static string FormatUrl(string endpoint, string path, string param)
        {
            return String.IsNullOrEmpty(param)
                ? FormatUrl(endpoint, path)
                : FormatUrl(endpoint, path) + "?" + param;
        }
    }
}
