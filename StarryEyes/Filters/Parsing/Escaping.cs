
namespace StarryEyes.Filters.Parsing
{
    internal static class Escaping
    {
        /// <summary>
        /// Escape string for querification.
        /// </summary>
        public static string EscapeForQuery(this string unescaped)
        {
            // \ => \\
            // " => \"
            return unescaped.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        /// <summary>
        /// Unescape string for parsing query.
        /// </summary>
        public static string UnescapeFromQuery(this string escaped)
        {
            // \" => "
            // \\ => \
            return escaped.Replace("\\\"", "\"").Replace("\\\\", "\\");
        }

        /// <summary>
        /// Quote string.
        /// </summary>
        /// <param name="escaped">escaping string</param>
        public static string Quote(this string escaped)
        {
            return "\"" + escaped + "\"";
        }

        /// <summary>
        /// Wrap with parenthesis.
        /// </summary>
        /// <param name="str">body</param>
        /// <returns>(body)</returns>
        public static string EnumerationToSelectClause(this string str)
        {
            return "(select (" + str + "))";
        }

        public static string Unparenthesis(this string str)
        {
            return str.StartsWith("(") && str.EndsWith(")") ? str.Substring(1, str.Length - 2) : str;
        }
    }
}