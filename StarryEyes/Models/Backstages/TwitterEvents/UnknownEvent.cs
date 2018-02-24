using Cadena.Data;

namespace StarryEyes.Models.Backstages.TwitterEvents
{
    public class UnknownEvent : TwitterEventBase
    {
        public UnknownEvent(TwitterUser source, string evstr)
            : base(source, source)
        {
            Detail = evstr;
        }

        public override string Title => "UNKNOWN EVENT";

        public override string Detail { get; }
    }
}