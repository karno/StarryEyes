using System;

namespace StarryEyes.Globalization
{
    public static class GlobalizationHelper
    {
        public static string SafeFormat(this string format, params object[] formatObjects)
        {
            try
            {
                return String.Format(format, formatObjects);
            }
            catch (FormatException fex)
            {
                return "[Format Error]" + format + "(" + fex.Message + ")";
            }
        }
    }
}
