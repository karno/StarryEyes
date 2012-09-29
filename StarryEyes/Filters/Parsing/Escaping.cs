
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
        /// <param name="argstr">escaped string</param>
        public static string Quote(this string escaped)
        {
            return "\"" + escaped + "\"";
        }
    }
}