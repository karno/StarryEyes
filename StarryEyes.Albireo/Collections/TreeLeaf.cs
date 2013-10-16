
namespace StarryEyes.Albireo.Collections
{
    internal class TreeLeaf<TValue, TLeaf> where TLeaf : TreeLeaf<TValue, TLeaf>
    {
        /// <summary>
        /// Value of this leaf.
        /// </summary>
        public TValue Value { get; set; }

        /// <summary>
        /// Leaf of the left.
        /// </summary>
        public TLeaf LeftLeaf { get; set; }

        /// <summary>
        /// Leaf of the right.
        /// </summary>
        public TLeaf RightLeaf { get; set; }
    }
}
