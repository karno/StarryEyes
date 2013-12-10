﻿using System;
using System.Collections.Generic;
using StarryEyes.Anomaly.TwitterApi.DataModels;

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
            return _ => _.GetOriginal().GetEntityAidedText();
        }

        public override string GetStringSqlQuery()
        {
            return "Text";
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
            // Using not original.
            return _ => _.Source ?? String.Empty;
        }

        public override string GetStringSqlQuery()
        {
            return Coalesce("Source", "");
        }

        public override string ToQuery()
        {
            return "via"; // source, from is also ok
        }
    }
}
