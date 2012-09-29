using System;
using System.Collections.Generic;
using StarryEyes.Moon.Api.Parsing;
using StarryEyes.Moon.Authorize;
using StarryEyes.Moon.DataModel;

namespace StarryEyes.Moon.Api.Rest
{
    public static class Timelines
    {
        public static IObservable<TwitterStatus> GetHomeTimeline(this AuthenticateInfo info,
            int count = 20, long? since_id = null, long? max_id = null, int? page = null,
            bool trim_user = false, bool include_rts = false, bool include_entities = true,
            bool exclude_replies = false, bool contributor_details = false)
        {
            var param = new Dictionary<string, object>()
            {
                {"count", count},
                {"since_id", since_id},
                {"max_id", max_id},
                {"page", page},
                {"trim_user", trim_user},
                {"include_rts", include_rts},
                {"include_entities", include_entities},
                {"exclude_replies", exclude_replies},
                {"contributor_details", contributor_details},
            }.Parametalize();
            return info.GetOAuthClient()
                .SetParameters(param)
                .SetEndpoint(ApiEndpoint.EndpointApiV1.JoinUrl("/statuses/home_timeline.json"))
                .GetResponse()
                .UpdateRateLimitInfo(info)
                .ReadTimeline();
        }

        public static IObservable<TwitterStatus> GetUserTimeline(this AuthenticateInfo info,
            long? user_id = null, string screen_name = null,
            int count = 20, long? since_id = null, long? max_id = null, int? page = null,
            bool trim_user = false, bool include_rts = true, bool include_entities = true,
            bool exclude_replies = false, bool contributor_details = false)
        {
            if (user_id == null && screen_name == null)
                throw new ArgumentNullException("both of user_id and screen_name are null.");
            var param = new Dictionary<string, object>()
            {
                {"user_id", user_id},
                {"screen_name", screen_name},
                {"count", count},
                {"since_id", since_id},
                {"max_id", max_id},
                {"page", page},
                {"trim_user", trim_user},
                {"include_rts", include_rts},
                {"include_entities", include_entities},
                {"exclude_replies", exclude_replies},
                {"contributor_details", contributor_details},
            }.Parametalize();
            return info.GetOAuthClient()
                .SetParameters(param)
                .SetEndpoint(ApiEndpoint.EndpointApiV1.JoinUrl("/statuses/user_timeline.json"))
                .GetResponse()
                .UpdateRateLimitInfo(info)
                .ReadTimeline();
        }

        public static IObservable<TwitterStatus> GetMentions(this AuthenticateInfo info,
            int count = 20, long? since_id = null, long? max_id = null, int? page = null,
            bool trim_user = false, bool include_rts = false, bool include_entities = true,
            bool contributor_details = false)
        {
            var param = new Dictionary<string, object>()
            {
                {"count", count},
                {"since_id", since_id},
                {"max_id", max_id},
                {"page", page},
                {"trim_user", trim_user},
                {"include_rts", include_rts},
                {"include_entities", include_entities},
                {"contributor_details", contributor_details},
            }.Parametalize();
            return info.GetOAuthClient()
                .SetParameters(param)
                .SetEndpoint(ApiEndpoint.EndpointApiV1.JoinUrl("/statuses/mentions.json"))
                .GetResponse()
                .UpdateRateLimitInfo(info)
                .ReadTimeline();
        }

        public static IObservable<TwitterStatus> GetRetweeetedByMe(this AuthenticateInfo info,
            int count = 20, long? since_id = null, long? max_id = null, int? page = null,
            bool trim_user = false, bool include_entities = true)
        {
            var param = new Dictionary<string, object>()
            {
                {"count", count},
                {"since_id", since_id},
                {"max_id", max_id},
                {"page", page},
                {"trim_user", trim_user},
                {"include_entities", include_entities},
            }.Parametalize();
            return info.GetOAuthClient()
                .SetParameters(param)
                .SetEndpoint(ApiEndpoint.EndpointApiV1.JoinUrl("/statuses/retweeted_by_me.json"))
                .GetResponse()
                .UpdateRateLimitInfo(info)
                .ReadTimeline();
        }

        public static IObservable<TwitterStatus> GetRetweeetedToMe(this AuthenticateInfo info,
            int count = 20, long? since_id = null, long? max_id = null, int? page = null,
            bool trim_user = false, bool include_entities = true)
        {
            var param = new Dictionary<string, object>()
            {
                {"count", count},
                {"since_id", since_id},
                {"max_id", max_id},
                {"page", page},
                {"trim_user", trim_user},
                {"include_entities", include_entities},
            }.Parametalize();
            return info.GetOAuthClient()
                .SetParameters(param)
                .SetEndpoint(ApiEndpoint.EndpointApiV1.JoinUrl("/statuses/retweeted_to_me.json"))
                .GetResponse()
                .UpdateRateLimitInfo(info)
                .ReadTimeline();
        }

        public static IObservable<TwitterStatus> GetRetweeetedOfMe(this AuthenticateInfo info,
            int count = 20, long? since_id = null, long? max_id = null, int? page = null,
            bool trim_user = false, bool include_entities = true)
        {
            var param = new Dictionary<string, object>()
            {
                {"count", count},
                {"since_id", since_id},
                {"max_id", max_id},
                {"page", page},
                {"trim_user", trim_user},
                {"include_entities", include_entities},
            }.Parametalize();
            return info.GetOAuthClient()
                .SetParameters(param)
                .SetEndpoint(ApiEndpoint.EndpointApiV1.JoinUrl("/statuses/retweeted_of_me.json"))
                .GetResponse()
                .UpdateRateLimitInfo(info)
                .ReadTimeline();
        }

        /// <summary>
        /// WARNING: this method is implemented, but this is maybe not work.
        /// </summary>
        public static IObservable<TwitterStatus> GetRetweeetedToUser(this AuthenticateInfo info,
            long? user_id = null, string screen_name = null,
            int count = 20, long? since_id = null, long? max_id = null, int? page = null,
            bool trim_user = false, bool include_entities = true)
        {
            if (user_id == null && screen_name == null)
                throw new ArgumentNullException("both of user_id and screen_name are null.");
            var param = new Dictionary<string, object>()
            {
                {"user_id", user_id},
                {"screen_name", screen_name},
                {"count", count},
                {"since_id", since_id},
                {"max_id", max_id},
                {"page", page},
                {"trim_user", trim_user},
                {"include_entities", include_entities},
            }.Parametalize();
            return info.GetOAuthClient()
                .SetParameters(param)
                .SetEndpoint(ApiEndpoint.EndpointApiV1.JoinUrl("/statuses/retweeted_to_user.json"))
                .GetResponse()
                .UpdateRateLimitInfo(info)
                .ReadTimeline();
        }

        /// <summary>
        /// WARNING: this method is implemented, but this is maybe not work.
        /// </summary>
        public static IObservable<TwitterStatus> GetRetweeetedByUser(this AuthenticateInfo info,
            long? user_id = null, string screen_name = null,
            int count = 20, long? since_id = null, long? max_id = null, int? page = null,
            bool trim_user = false, bool include_entities = true)
        {
            if (user_id == null && screen_name == null)
                throw new ArgumentNullException("both of user_id and screen_name are null.");
            var param = new Dictionary<string, object>()
            {
                {"user_id", user_id},
                {"screen_name", screen_name},
                {"count", count},
                {"since_id", since_id},
                {"max_id", max_id},
                {"page", page},
                {"trim_user", trim_user},
                {"include_entities", include_entities},
            }.Parametalize();          
            return info.GetOAuthClient()
                .SetParameters(param)
                .SetEndpoint(ApiEndpoint.EndpointApiV1.JoinUrl("/statuses/retweeted_by_user.json"))
                .GetResponse()
                .UpdateRateLimitInfo(info)
                .ReadTimeline();
        }
    }
}
