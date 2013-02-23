using System;
using System.Collections.Generic;
using StarryEyes.Breezy.Api.Parsing;
using StarryEyes.Breezy.Authorize;
using StarryEyes.Breezy.DataModel;

namespace StarryEyes.Breezy.Api.Rest
{
    public static class DirectMessages
    {
        public static IObservable<TwitterStatus> GetDirectMessages(this AuthenticateInfo info,
            long? since_id = null, long? max_id = null, int count = 20, int? page = null,
            bool include_entities = true, bool? skip_status = null)
        {
            var param = new Dictionary<string, object>()
            {
                {"since_id", since_id},
                {"max_id", max_id},
                {"count", count},
                {"page", page},
                {"include_entities", include_entities},
                {"skip_status", skip_status}
            }.Parametalize();
            return info.GetOAuthClient()
                .SetEndpoint(ApiEndpoint.EndpointApiV1a.JoinUrl("/direct_messages.json"))
                .SetParameters(param)
                .GetResponse()
                .UpdateRateLimitInfo(info)
                .ReadDirectMessages();
        }

        public static IObservable<TwitterStatus> GetSentDirectMessages(this AuthenticateInfo info,
            long? since_id = null, long? max_id = null, int count = 20, int? page = null,
            bool include_entities = true)
        {
            var param = new Dictionary<string, object>()
            {
                {"since_id", since_id},
                {"max_id", max_id},
                {"count", count},
                {"page", page},
                {"include_entities", include_entities},
            }.Parametalize();
            return info.GetOAuthClient()
                .SetEndpoint(ApiEndpoint.EndpointApiV1a.JoinUrl("/direct_messages/sent.json"))
                .SetParameters(param)
                .GetResponse()
                .UpdateRateLimitInfo(info)
                .ReadDirectMessages();
        }

        public static IObservable<TwitterStatus> DestroyDirectMessage(this AuthenticateInfo info,
            long id, bool include_entities = true)
        {
            var param = new Dictionary<string, object>()
            {
                {"include_entities", include_entities},
                {"id", id},
            }.Parametalize();
            return info.GetOAuthClient()
                .SetEndpoint(ApiEndpoint.EndpointApiV1a.JoinUrl("/direct_messages/destroy.json"))
                .SetMethodType(Codeplex.OAuth.MethodType.Post)
                .SetParameters(param)
                .GetResponse()
                .ReadDirectMessage();
        }

        public static IObservable<TwitterStatus> SendDirectMessage(this AuthenticateInfo info,
            string text, long? user_id = null, string screen_name = null)
        {
            if (user_id == null && screen_name == null)
                throw new ArgumentNullException("both of user_id and screen_name are zero.");
            var param = new Dictionary<string, object>()
            {
                {"text", text},
                {"user_id", user_id},
                {"screen_name", screen_name},
            }.Parametalize();
            return info.GetOAuthClient()
                .SetEndpoint(ApiEndpoint.EndpointApiV1a.JoinUrl("/direct_messages/new.json"))
                .SetMethodType(Codeplex.OAuth.MethodType.Post)
                .SetParameters(param)
                .GetResponse()
                .ReadDirectMessage();
        }

        public static IObservable<TwitterStatus> ShowDirectMessage(this AuthenticateInfo info,
            long id)
        {
            var param = new Dictionary<string, object>()
            {
                {"id", id}
            }.Parametalize();
            return info.GetOAuthClient()
                .SetEndpoint(ApiEndpoint.EndpointApiV1a.JoinUrl("/direct_messages/show.json"))
                .SetMethodType(Codeplex.OAuth.MethodType.Get)
                .SetParameters(param)
                .GetResponse()
                .UpdateRateLimitInfo(info)
                .ReadDirectMessage();
        }
    }
}
