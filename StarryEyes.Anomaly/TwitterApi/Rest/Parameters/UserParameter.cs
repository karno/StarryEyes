using System.Collections.Generic;

namespace StarryEyes.Anomaly.TwitterApi.Rest.Parameters
{
    public sealed class UserParameter : ParameterBase
    {
        public long? UserId { get; private set; }

        public string ScreenName { get; private set; }

        private string _userIdKey = "user_id";
        private string _screenNameKey = "screen_name";

        internal string UserIdKey
        {
            get { return _userIdKey; }
            set { _userIdKey = value; }
        }

        internal string ScreenNameKey
        {
            get { return _screenNameKey; }
            set { _screenNameKey = value; }
        }

        internal void SetKeyAsSource()
        {
            UserIdKey = "source_id";
            ScreenNameKey = "source_screen_name";
        }

        internal void SetKeyAsTarget()
        {
            UserIdKey = "target_id";
            ScreenNameKey = "target_screen_name";
        }

        public UserParameter(long userId)
        {
            UserId = userId;
            ScreenName = null;
        }

        public UserParameter(string screenName)
        {
            UserId = null;
            ScreenName = screenName;
        }

        public override void SetDictionary(Dictionary<string, object> target)
        {
            target[UserIdKey] = UserId;
            target[ScreenNameKey] = ScreenName;
        }
    }
}
