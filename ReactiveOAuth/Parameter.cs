using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Codeplex.OAuth
{
    /// <summary>represents query parameter(Key and Value)</summary>
    public class Parameter
    {
        public string Key { get; private set; }
        public string Value { get; private set; }

        public Parameter(string key, object value)
        {
            Guard.ArgumentNull(key, "key");
            Guard.ArgumentNull(value, "value");

            this.Key = key;
            this.Value = value.ToString();
        }

        /// <summary>UrlEncode(Key)=UrlEncode(Value)</summary>
        public override string ToString()
        {
            return Key.UrlEncode() + "=" + Value.UrlEncode();
        }
    }

    /// <summary>represents query parameter(Key and Value) collection</summary>
    public class ParameterCollection : IEnumerable<Parameter>
    {
        private List<Parameter> list = new List<Parameter>();

        public void Add(Parameter parameter)
        {
            Guard.ArgumentNull(parameter, "parameter");
            list.Add(parameter);
        }

        public void Add(IEnumerable<Parameter> parameters)
        {
            Guard.ArgumentNull(parameters, "parameters");
            list.AddRange(parameters);
        }

        public void Add(string key, object value)
        {
            list.Add(new Parameter(key, value));
        }

        public IEnumerator<Parameter> GetEnumerator()
        {
            return list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public static class ParametersExtension
    {
        /// <summary>convert urlencoded querystring</summary>
        public static string ToQueryParameter(this IEnumerable<Parameter> parameters)
        {
            return parameters.Select(p => p.ToString()).ToString("&");
        }
    }
}