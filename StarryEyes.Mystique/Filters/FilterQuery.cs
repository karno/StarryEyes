using System;
using System.Linq;
using StarryEyes.Mystique.Filters.Expressions;
using StarryEyes.Mystique.Filters.Sources;
using StarryEyes.SweetLady.DataModel;

namespace StarryEyes.Mystique.Filters
{
    public sealed class FilterQuery
    {
        public FilterSourceBase[] Sources;

        public FilterExpressionRoot PredicateTreeRoot;

        public string ToQuery()
        {
            return "from " + Sources.GroupBy(s => s.FilterKey)
                .Select(g => g.Distinct(_ => _.FilterValue))
                .Select(f => {
                    if (f.Count() == 1)
                    {
                        var item = f.First();
                        if (String.IsNullOrEmpty(item.FilterValue))
                            return item.FilterKey;
                        else
                            return item.FilterKey + ": " + item.FilterValue;
                    }
                    else
                    {
                        return f.First().FilterKey + ": " + f.Select(fs => "\"" + fs.FilterValue + "\"").JoinString(", ");
                    }
                })
                .JoinString(", ") +
                " where " + PredicateTreeRoot.ToQuery();
        }

        public Func<TwitterStatus, bool> GetEvaluator()
        {
            var sourcesEvals = Sources.Select(s => s.GetEvaluator());
            var predEvals = PredicateTreeRoot.GetEvaluator();
            return _ => sourcesEvals.Any(f => f(_)) && predEvals(_);
        }
    }
}
