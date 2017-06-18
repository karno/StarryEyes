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

            // distinct by startindex ignores extended_entities.
            foreach (var entity in entities.Distinct(e => e.StartIndex).OrderBy(e => e.StartIndex))
            {
                if (endIndex < entity.StartIndex)
                {
                    // return raw string
                    yield return new TextEntityDescription(ParsingExtension.ResolveEntity(
                        escaped.SurrogatedSubstring(endIndex, entity.StartIndex - endIndex)));
                }
                if (escaped.Length <= entity.StartIndex || escaped.Length < entity.EndIndex)
                {
                    // Twitter rarely gives entities of extended_tweet for text of compatibility mode.
                    // We need paranoiac test here.
                    continue;
                }
                // get entitied text
                var body = ParsingExtension.ResolveEntity(escaped.SurrogatedSubstring(
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
                    escaped.SurrogatedSubstring(endIndex)));
            }
        }

        // below code from Mystique pull request #53.
        // Thanks for Hotspring-r
        // https://github.com/karno/Mystique/commit/a8d174bcfe9292290bd9058ecf7ce2b68dc4162e
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
