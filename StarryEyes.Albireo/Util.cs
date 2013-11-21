using System;

namespace StarryEyes.Albireo
{
    public static class EventUtil
    {
        public static void SafeInvoke(this Action action)
        {
            if (action != null) action();
        }

        public static void SafeInvoke<T>(this Action<T> action, T arg)
        {
            if (action != null) action(arg);
        }

        public static void SafeInvoke<T1, T2>(this Action<T1, T2> action, T1 arg1, T2 arg2)
        {
            if (action != null) action(arg1, arg2);
        }
    }
}
