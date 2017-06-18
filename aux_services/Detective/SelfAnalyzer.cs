
using System;
using System.Text;
using Detective.Properties;

namespace Detective
{
    public static class SelfAnalyzer
    {
        public static string AnalyzeResult { get; private set; }

        public static bool Analyze(string log)
        {
            var result = new StringBuilder();
            if ((log.Contains("System.IO.FileLoadException", StringComparison.CurrentCultureIgnoreCase) ||
                 log.Contains("System.IO.FileNotFoundException", StringComparison.CurrentCultureIgnoreCase)) &&
                log.Contains("PublicKeyToken", StringComparison.CurrentCultureIgnoreCase))
            {
                result.AppendLine(Resources.EssentialFileIsMissing);
                result.InsertDelimiter();
            }

            if (log.Contains("System.Data.SQLite.SQLiteException", StringComparison.CurrentCultureIgnoreCase) ||
                log.Contains("StarryEyes.Casket.SqliteCrudException", StringComparison.CurrentCultureIgnoreCase))
            {
                // database error
                if (log.Contains("locking protocol", StringComparison.CurrentCultureIgnoreCase))
                {
                    result.AppendLine(Resources.DatabaseIsLocked);
                    result.InsertDelimiter();
                }
                else if (log.Contains("database disk image is malformed", StringComparison.CurrentCultureIgnoreCase))
                {
                    result.AppendLine(Resources.DatabaseImageIsMalformed);
                    result.InsertDelimiter();
                }
            }

            AnalyzeResult = result.ToString();
            return AnalyzeResult.Length > 0;
        }

        private static void InsertDelimiter(this StringBuilder builder)
        {
            builder.AppendLine("----------");
        }

        private static bool Contains(this string haystack, string neeedle, StringComparison comparison)
        {
            return haystack.IndexOf(neeedle, comparison) >= 0;
        }
    }
}
