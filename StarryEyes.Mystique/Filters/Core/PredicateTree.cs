
namespace StarryEyes.Mystique.Filters.Core
{
    /// <summary>
    /// Predicate tree
    /// </summary>
    public sealed class PredicateTree : PredicateBase
    {
        /// <summary>
        /// Concatenate by AND predicate
        /// </summary>
        public bool And { get; set; }

        /// <summary>
        /// Left leaf of predicate
        /// </summary>
        public PredicateBase Left { get; set; }

        /// <summary>
        /// Right leaf of predicate
        /// </summary>
        public PredicateBase Right { get; set; }

        protected override bool FilterCore(SweetLady.DataModel.TwitterStatus status)
        {
            return And
                ? (Left.Filter(status) && Right.Filter(status))
                : (Left.Filter(status) || Right.Filter(status));
        }

        public override string ToQuery()
        {
            return "(" + Left.ToQuery() +
                (And ? " && " : " || ") +
                Right.ToQuery() + ")";
        }
    }
}
