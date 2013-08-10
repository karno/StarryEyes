using System;
using System.Linq;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Filters.Expressions;
using StarryEyes.Filters.Sources;

namespace StarryEyes.Filters
{
    public sealed class FilterQuery : IEquatable<FilterQuery>
    {
        public event Action InvalidateRequired;
        private void RaiseInvalidateRequired()
        {
            var handler = this.InvalidateRequired;
            if (handler != null)
                handler();
        }

        public FilterSourceBase[] Sources { get; set; }

        public FilterExpressionRoot PredicateTreeRoot { get; set; }

        public string ToQuery()
        {
            return "from " + Sources.GroupBy(s => s.FilterKey)
                .Select(g => g.Distinct(_ => _.FilterValue).ToArray())
                .Select(fs =>
                {
                    if (fs.Length == 1)
                    {
                        var item = fs[0];
                        if (String.IsNullOrEmpty(item.FilterValue))
                            return item.FilterKey;
                        return item.FilterKey + ": \"" + item.FilterValue + "\"";
                    }
                    return fs[0].FilterKey + ": " + fs.Select(f => "\"" + f.FilterValue + "\"").JoinString(", ");
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

        public bool Equals(FilterQuery other)
        {
            if (other == null) return false;
            return this.ToQuery() == other.ToQuery();
        }

        // override object.Equals
        public override bool Equals(object obj)
        {
            return Equals(obj as FilterQuery);
        }

        // override object.GetHashCode
        public override int GetHashCode()
        {
            return ToQuery().GetHashCode();
        }

        public override string ToString()
        {
            return ToQuery();
        }

        public void Activate()
        {
            if (Sources != null)
            {
                Sources.ForEach(s => s.Activate());
            }
            if (this.PredicateTreeRoot == null) return;
            this.PredicateTreeRoot.BeginLifecycle();
            this.PredicateTreeRoot.ReapplyRequested += this.RaiseInvalidateRequired;
        }

        public void Deactivate()
        {
            if (Sources != null)
            {
                Sources.ForEach(s => s.Deactivate());
            }
            if (this.PredicateTreeRoot == null) return;
            this.PredicateTreeRoot.EndLifecycle();
            this.PredicateTreeRoot.ReapplyRequested -= this.RaiseInvalidateRequired;
        }
    }
}
