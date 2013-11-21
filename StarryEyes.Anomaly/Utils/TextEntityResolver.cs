using System;
using System.Collections.Generic;
using System.Linq;
using StarryEyes.Anomaly.TwitterApi.DataModels;

namespace StarryEyes.Anomaly.Utils
{
    public static class TextEntityResolver
    {
        public static IEnumerable<TextEntityDescription> ParseText(TwitterStatus status)
        {
            if (status == null) throw new ArgumentNullException("status");
            return ParseText(status.Text, status.Entities.Guard());
        }

        public static IEnumerable<TextEntityDescription> ParseText(
            string text, IEnumerable<TwitterEntity> entities)
        {
            if (entities == null) throw new ArgumentNullException("entities");
            var escaped = ParsingExtension.EscapeEntity(text);
            var endIndex = 0;

            foreach (var entity in entities.OrderBy(e => e.StartIndex))
            {
                if (endIndex < entity.StartIndex)
                {
                    // return raw string
                    yield return new TextEntityDescription(ParsingExtension.ResolveEntity(
                        escaped.SubstringForSurrogatePaire(endIndex, entity.StartIndex - endIndex)));
                }
                // get entitied text
                var body = ParsingExtension.ResolveEntity(escaped.SubstringForSurrogatePaire(
                    entity.StartIndex, entity.EndIndex - entity.StartIndex));
                yield return new TextEntityDescription(body, entity);
                endIndex = entity.EndIndex;
            }
            if (endIndex == 0)
            {
                // entity is empty.
                yield return new TextEntityDescription(text);
            }
            else if (endIndex < escaped.Length)
            {
                // return remain text
                yield return new TextEntityDescription(ParsingExtension.ResolveEntity(
                    escaped.SubstringForSurrogatePaire(endIndex)));
            }
        }

        // below code from Mystique pull request #53.
        // Thanks for Hotspring-r
        // https://github.com/karno/Mystique/commit/a8d174bcfe9292290bd9058ecf7ce2b68dc4162e

        /// <summary>
        /// Pick substring from string considering surrogate pairs.
        /// </summary>
        /// <param name="str">source text</param>
        /// <param name="startIndex">start index</param>
        /// <param name="length">cut length</param>
        /// <returns>substring of text</returns>
        public static string SubstringForSurrogatePaire(this string str, int startIndex, int length = -1)
        {
            if (str == null) throw new ArgumentNullException("str");
            var s = GetLengthForSurrogatePaire(str, startIndex, 0);

            if (length == -1)
            {
                return str.Substring(s);
            }

            var l = GetLengthForSurrogatePaire(str, length, s);
            return str.Substring(s, l);
        }

        private static int GetLengthForSurrogatePaire(string str, int len, int s)
        {
            var l = 0;
            for (var i = 0; i < len; i++)
            {
                if (Char.IsHighSurrogate(str[l + s]))
                {
                    l++;
                }
                l++;
            }
            return l;
        }
    }

    public class TextEntityDescription
    {
        private readonly string _text;
        private readonly TwitterEntity _entity;

        public TextEntityDescription(string text, TwitterEntity entity = null)
        {
            _text = text;
            _entity = entity;
        }

        public bool IsEntityAvailable { get { return Entity != null; } }

        public string Text
        {
            get { return _text; }
        }

        public TwitterEntity Entity
        {
            get { return _entity; }
        }
    }
}
