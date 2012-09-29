using System;
using System.Collections.Generic;
using StarryEyes.Moon.DataModel;

namespace StarryEyes.Filters.Expressions.Values.Users
{
    public sealed class User : ValueBase
    {
        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get
            {
                yield return FilterExpressionType.Numeric;
                yield return FilterExpressionType.String;
            }
        }

        public override Func<TwitterStatus, long> GetNumericValueProvider()
        {
            return _ => _.GetOriginal().User.Id;
        }

        public override Func<TwitterStatus, string> GetStringValueProvider()
        {
            return _ => _.GetOriginal().User.ScreenName;
        }

        public override string ToQuery()
        {
            return "user";
        }
    }

    public sealed class Retweeter : ValueBase
    {
        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get
            {
                yield return FilterExpressionType.Numeric;
                yield return FilterExpressionType.String;
            }
        }

        public override Func<TwitterStatus, long> GetNumericValueProvider()
        {
            return _ => _.RetweetedOriginal != null ? _.User.Id : -1;
        }

        public override Func<TwitterStatus, string> GetStringValueProvider()
        {
            return _ => _.RetweetedOriginal != null ? _.User.ScreenName : String.Empty;
        }

        public override string ToQuery()
        {
            return "retweeter";
        }
    }
}
