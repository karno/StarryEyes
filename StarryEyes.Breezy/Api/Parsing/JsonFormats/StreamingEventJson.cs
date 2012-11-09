using Newtonsoft.Json;
using StarryEyes.Breezy.DataModel;

namespace StarryEyes.Breezy.Api.Parsing.JsonFormats
{
    public class StreamingEventJson : ITwitterStreamingElementSpawnable
    {
        [JsonProperty("event")]
        public string event_kind { get; set; }

        public virtual TwitterStreamingElement Spawn()
        {
            return new TwitterStreamingElement()
            {
                EventType = event_kind.ToEventType()
            };
        }
    }

    public class StreamingUserEventJson : StreamingEventJson
    {
        public UserJson source { get; set; }

        public UserJson target { get; set; }

        public string created_at { get; set; }

        public override TwitterStreamingElement Spawn()
        {
            return new TwitterStreamingElement()
            {
                EventType = event_kind.ToEventType(),
                EventSourceUser = source != null ? source.Spawn() : null,
                EventTargetUser = target != null ? target.Spawn() : null,
            };
        }
    }

    public class StreamingTweetEventJson : StreamingUserEventJson
    {
        public TweetJson target_object { get; set; }

        public override TwitterStreamingElement Spawn()
        {
            return new TwitterStreamingElement()
            {
                EventType = event_kind.ToEventType(),
                EventSourceUser = source != null ? source.Spawn() : null,
                EventTargetUser = target != null ? target.Spawn() : null,
                EventTargetTweet = target_object != null ? target_object.Spawn() : null,
            };
        }
    }

    public class StreamingListEventJson : StreamingUserEventJson
    {
        public ListJson target_object { get; set; }

        public override TwitterStreamingElement Spawn()
        {
            return new TwitterStreamingElement()
            {
                EventType = event_kind.ToEventType(),
                EventSourceUser = source != null ? source.Spawn() : null,
                EventTargetUser = target != null ? target.Spawn() : null,
                EventTargetList = target_object != null ? target_object.Spawn() : null,
            };
        }
    }
}
