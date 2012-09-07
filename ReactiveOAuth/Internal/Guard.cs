using System;

namespace Codeplex.OAuth
{
    internal static class Guard
    {
        public static void ArgumentNull<T>(T target, string paramName)
        {
            if (target == null) throw new ArgumentNullException(paramName);
        }
    }
}