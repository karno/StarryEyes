using System;
using System.Diagnostics;
using StarryEyes.Settings;

namespace StarryEyes.Helpers
{
    public static class BrowserHelper
    {
        public static void Open(string url)
        {
            try
            {
                if (String.IsNullOrEmpty(Setting.ExternalBrowserPath.Value))
                    Process.Start(url);
                else
                    Process.Start(Setting.ExternalBrowserPath.Value, url);
            }
            catch { }
        }

        public static void Open(Uri uri)
        {
            Open(uri.OriginalString);
        }
    }
}
