using System;

namespace StarryEyes.Albireo
{
    public static class Util
    {
        public static bool IsNullOrEmpty(this string text)
        {
            return String.IsNullOrEmpty(text);
        }

        public static bool IsNullOrWhiteSpace(this string text)
        {
            return String.IsNullOrWhiteSpace(text);
        }
    }
}
