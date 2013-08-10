using StarryEyes.Anomaly.TwitterApi.DataModels;

namespace StarryEyes.Models.Backstages.TwitterEvents
{
    public class UnknownEvent : TwitterEventBase
    {
        private readonly string _evstr;

        public UnknownEvent(TwitterUser source, string evstr)
            : base(source, source)
        {
            _evstr = evstr;
        }

        public override string Title
        {
            get { return "UNKNOWN EVENT"; }
        }

        public override string Detail
        {
            get { return _evstr; }
        }
    }
}
