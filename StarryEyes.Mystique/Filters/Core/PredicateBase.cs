using StarryEyes.SweetLady.DataModel;

namespace StarryEyes.Mystique.Filters.Core
{
    /// <summary>
    /// Base of a predicate expression
    /// </summary>
    public abstract class PredicateBase
    {
        /// <summary>
        /// Filter the status.
        /// </summary>
        /// <param name="status">target status</param>
        /// <returns>whether filter or not</returns>
        public bool Filter(TwitterStatus status)
        {
            return FilterCore(status) != Negate;
        }

        /// <summary>
        /// Whether this filter is negated or not.
        /// </summary>
        public bool Negate { get; set; }

        /// <summary>
        /// Filter core implementation.<para />
        /// this is not to mention to filtering.
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        protected abstract bool FilterCore(TwitterStatus status);

        /// <summary>
        /// Convert back to query
        /// </summary>
        public abstract string ToQuery();
    }
}
