using System;
using Cadena.Api.Parameters;

namespace StarryEyes.Models.Receiving
{
    public class ListInfo : IEquatable<ListInfo>, IComparable<ListInfo>, IComparable
    {
        public ListInfo()
        {
        }

        public ListInfo(string screenName, string slug)
        {
            OwnerScreenName = screenName;
            Slug = slug;
        }

        public ListInfo(ListParameter parameter)
        {
            OwnerScreenName = parameter.OwnerScreenName;
            Slug = parameter.Slug;
        }

        public string Slug { get; set; }

        public string OwnerScreenName { get; set; }

        public override bool Equals(object obj)
        {
            return Equals(obj as ListInfo);
        }

        public bool Equals(ListInfo other)
        {
            if (other == null) return false;
            return other.OwnerScreenName.Equals(OwnerScreenName, StringComparison.CurrentCultureIgnoreCase) &&
                   other.Slug.Equals(Slug, StringComparison.CurrentCultureIgnoreCase);
        }

        public override int GetHashCode()
        {
            return OwnerScreenName.GetHashCode() ^ Slug.GetHashCode();
        }

        public int CompareTo(object obj)
        {
            return CompareTo((ListInfo)obj);
        }

        public int CompareTo(ListInfo other)
        {
            return String.Compare(
                OwnerScreenName + "/" + Slug,
                other.OwnerScreenName + "/" + other.Slug,
                StringComparison.Ordinal);
        }

        public override string ToString()
        {
            return OwnerScreenName + "/" + Slug;
        }

        public ListParameter ToParameter()
        {
            return new ListParameter(OwnerScreenName, Slug);
        }
    }
}