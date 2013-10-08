
namespace StarryEyes.Filters.Expressions
{
    public enum FilterExpressionType
    {
        /// <summary>
        /// Invalid type. please do not use this.
        /// </summary>
        Invalid,
        /// <summary>
        /// Boolean value
        /// </summary>
        Boolean,
        /// <summary>
        /// Numeric value
        /// </summary>
        Numeric,
        /// <summary>
        /// Text string
        /// </summary>
        String,
        /// <summary>
        /// Numerical set (has 0~N Elements)
        /// </summary>
        Set,
    }
}
