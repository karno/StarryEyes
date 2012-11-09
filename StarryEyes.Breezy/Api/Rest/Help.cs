using System;
using System.Reactive.Linq;
using StarryEyes.Breezy.Api.Parsing;
using StarryEyes.Breezy.Api.Parsing.JsonFormats;
using StarryEyes.Breezy.Authorize;

namespace StarryEyes.Breezy.Api.Rest
{
    public static class Help
    {
        public static IObservable<ConfigurationJson> GetConfiguration(this AuthenticateInfo info)
        {
            return info.GetOAuthClient()
                .SetEndpoint(ApiEndpoint.EndpointApiV1a.JoinUrl("/help/configuration.json"))
                .GetResponse()
                .UpdateRateLimitInfo(info)
                .ReadString()
                .DeserializeJson<ConfigurationJson>();
        }

        public static IObservable<LanguageJson> GetLanguages(this AuthenticateInfo info)
        {
            return info.GetOAuthClient()
                .SetEndpoint(ApiEndpoint.EndpointApiV1a.JoinUrl("/help/languages.json"))
                .GetResponse()
                .UpdateRateLimitInfo(info)
                .ReadString()
                .DeserializeJsonArray<LanguageJson>()
                .SelectMany(_ => _);
        }
    }
}
