using System.Collections.Generic;

namespace StarryEyes.Anomaly.TwitterApi.Rest.Parameter
{
    public abstract class ParameterBase
    {
        public abstract void SetDictionary(Dictionary<string, object> target);

        public Dictionary<string, object> ToDictionary()
        {
            var dict = new Dictionary<string, object>();
            this.SetDictionary(dict);
            return dict;
        }
    }
}
