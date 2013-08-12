using System;
using System.Diagnostics;
using System.Windows;

namespace StarryEyes.Helpers
{
    public static class DebugHelper
    {
        [Conditional("DEBUG")]
        public static void EnsureBackgroundThread()
        {
            if (Application.Current.Dispatcher.CheckAccess())
            {
                throw new InvalidOperationException("This operation could not run on dispatcher.");
            }
        }
    }
}
