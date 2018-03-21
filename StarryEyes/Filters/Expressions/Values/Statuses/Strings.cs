using System;
using System.Collections.Generic;
using Cadena.Data;
using StarryEyes.Helpers;

namespace StarryEyes.Filters.Expressions.Values.Statuses
{
    public sealed class StatusText : ValueBase
    {
        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get { yield return FilterExpressionType.String; }
        }

        public override Func<TwitterStatus, string> GetStringValueProvider()
        {
            return s => s.GetOriginal().GetEntityAidedText(EntityDisplayMode.FullText);
        }

        public override string GetStringSqlQuery()
        {
            return "EntityAidedText";
        }

        public override string ToQuery()
        {
            return "text";
        }
    }

    public sealed class StatusSource : ValueBase
    {
        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get { yield return FilterExpressionType.String; }
        }

        public override Func<TwitterStatus, string> GetStringValueProvider()
        {
            // Use retweeted original.
            return s => s.GetOriginal().Source ?? String.Empty;
        }

        public override string GetStringSqlQuery()
        {
            return Coalesce("BaseSource", "");
        }

        public override string ToQuery()
        {
            return "via"; // source, from is also ok
        }
    }
}