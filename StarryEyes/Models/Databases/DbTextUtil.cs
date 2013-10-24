using System.Linq;

namespace StarryEyes.Models.Databases
{
    public static class DbTextUtil
    {
        public static string CreateNgram(string source, int n)
        {
            if (source.Length <= n)
            {
                return source;
            }
            return Enumerable.Range(0, source.Length - n + 1)
                             .Select(i => source.Substring(i, n))
                             .JoinString(" ");
        }

        /// <summary>
        /// Wrap with parenthesis.
        /// </summary>
        /// <param name="str">body</param>
        /// <returns>(select (body))</returns>
        public static string EnumerationToSelectClause(this string str)
        {
            return "(select (" + (string.IsNullOrEmpty(str) ? "-1" : str) + "))";
        }

        /// <summary>
        /// Remove parenthesis, if parenthesised.
        /// </summary>
        /// <param name="str">(body) or body</param>
        /// <returns>body</returns>
        public static string Unparenthesis(this string str)
        {
            return str.StartsWith("(") && str.EndsWith(")") ? str.Substring(1, str.Length - 2) : str;
        }

        /// <summary>
        /// Concatenate two sql sentence with and.
        /// </summary>
        /// <param name="left">left sentence, empty, or null</param>
        /// <param name="right">right sentence, empty, or null</param>
        /// <returns>concatenated sql</returns>
        public static string SqlConcatAnd(this string left, string right)
        {
            if (string.IsNullOrEmpty(left)) return right;
            if (string.IsNullOrEmpty(right)) return left;
            return "(" + left + ") and (" + right + ")";
        }
    }
}
