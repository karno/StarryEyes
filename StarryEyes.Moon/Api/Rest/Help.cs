using System;
using System.Reactive.Linq;
using StarryEyes.Moon.Api.Parsing;
using StarryEyes.Moon.Api.Parsing.JsonFormats;
using StarryEyes.Moon.Authorize;

namespace StarryEyes.Moon.Api.Rest
{
    public static class Help
    {
        public static IObservable<bool> Test(this AuthenticateInfo info)
        {
            return info.GetOAuthClient()
                .SetEndpoint(ApiEndpoint.EndpointApiV1.JoinUrl("/help/test.json"))
                .GetResponseText()
                .Select(_ => _ == "\"ok\"");
        }

        public static IObservable<ConfigurationJson> GetConfiguration(this AuthenticateInfo info)
        {
            return info.GetOAuthClient()
                .SetEndpoint(ApiEndpoint.EndpointApiV1.JoinUrl("/help/configuration.json"))
                .GetResponse()
                .UpdateRateLimitInfo(info)
                .ReadString()
                .DeserializeJson<ConfigurationJson>();
        }

        public static IObservable<LanguageJson> GetLanguages(this AuthenticateInfo info)
        {
            return info.GetOAuthClient()
                .SetEndpoint(ApiEndpoint.EndpointApiV1.JoinUrl("/help/languages.json"))
                .GetResponse()
                .UpdateRateLimitInfo(info)
                .ReadString()
                .DeserializeJsonArray<LanguageJson>()
                .SelectMany(_ => _);
        }
    }
}
