using System.Text;

namespace Starcluster.Mapper
{
    public static class BigramTextGenerator
    {
        public static string Generate(string text)
        {
            if (text.Length <= 2)
            {
                return text;
            }
            var i = 1;
            var bigramBuilder = new StringBuilder(text.Length * 2);
            bigramBuilder.Append(text, 0, 2);
            for (; i < text.Length - 1; i++)
            {
                bigramBuilder.Append(" ");
                bigramBuilder.Append(text, i, 2);
            }
            bigramBuilder.Append(" ");
            bigramBuilder.Append(text, i, 1);
            return bigramBuilder.ToString();
        }
    }
}