using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using Cadena.Data;
using Cadena.Meteor;

namespace Starcluster.Models
{
    internal static class DbModelHelper
    {
        internal static void AssertCorrelation(TwitterStatus param, long? id, string paramName, string idName)
        {
            if (param == null)
            {
                if (id != null)
                {
                    throw new ArgumentNullException(paramName,
                        $"{paramName} should not be null when {idName} is not null.");
                }
            }
            else if (param.Id != id)
            {
                throw new ArgumentException($"id of {paramName} and {idName} is not matched", paramName);
            }
        }

        internal static void AssertCorrelation(TwitterUser param, long? id, string paramName, string idName)
        {
            if (param == null)
            {
                if (id != null)
                {
                    throw new ArgumentNullException(paramName,
                        $"{paramName} should not be null when {idName} is not null.");
                }
            }
            else if (param.Id != id)
            {
                throw new ArgumentException($"id of {paramName} and {idName} is not matched", paramName);
            }
        }

        internal static Tuple<T, T> CreateNullableTuple<T>(T? item1, T? item2) where T : struct
        {
            return item1 != null && item2 != null
                ? Tuple.Create(item1.Value, item2.Value)
                : null;
        }

        internal static Uri ToUriOrNull(this string uriString)
        {
            return Uri.TryCreate(uriString, UriKind.Absolute, out var uri) ? uri : null;
        }

        internal static string DictionaryToJson<T>(IReadOnlyDictionary<string, T> dict, Func<T, string> valueConverter)
        {
            var builder = new StringBuilder();
            foreach (var kvp in dict)
            {
                builder.Append($",\"{kvp.Key}\":{valueConverter(kvp.Value)}");
            }
            return "{" + builder.ToString(1, builder.Length - 1) + "}";
        }

        internal static IReadOnlyDictionary<string, T> JsonToDictionary<T>(string json,
            Func<JsonValue, T> valueConverter)
        {
            var values = new Dictionary<string, T>();
            var jsonobj = MeteorJson.Parse(json).AsObjectOrNull();
            if (jsonobj != null)
            {
                foreach (var item in jsonobj)
                {
                    values.Add(item.Key, valueConverter(item.Value));
                }
            }
            return new ReadOnlyDictionary<string, T>(values);
        }
    }
}