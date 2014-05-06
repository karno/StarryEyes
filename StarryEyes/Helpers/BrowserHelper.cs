using System;
using System.Diagnostics;
using StarryEyes.Settings;

// ReSharper disable once CheckNamespace
namespace StarryEyes
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
            // ReSharper disable once EmptyGeneralCatchClause
            catch { }
        }

        public static void Open(Uri uri)
        {
            Open(uri.OriginalString);
        }
    }
}
