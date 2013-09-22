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
    }
}
