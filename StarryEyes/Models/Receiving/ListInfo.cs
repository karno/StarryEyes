using System;

namespace StarryEyes.Models.Receiving
{
    public class ListInfo : IEquatable<ListInfo>, IComparable<ListInfo>, IComparable
    {
        public string Slug { get; set; }

        public string OwnerScreenName { get; set; }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as ListInfo);
        }

        public bool Equals(ListInfo other)
        {
            if (other == null) return false;
            return other.OwnerScreenName == this.OwnerScreenName && other.Slug == this.Slug;
        }

        public override int GetHashCode()
        {
            return this.OwnerScreenName.GetHashCode() ^ this.Slug.GetHashCode();
        }

        public int CompareTo(object obj)
        {
            return this.CompareTo((ListInfo)obj);
        }

        public int CompareTo(ListInfo other)
        {
            return String.Compare(
                this.OwnerScreenName + this.Slug,
                other.OwnerScreenName + other.Slug,
                StringComparison.Ordinal);
        }

        public override string ToString()
        {
            return this.OwnerScreenName + "/" + this.Slug;
        }
    }
}