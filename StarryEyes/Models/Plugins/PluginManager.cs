using System;
using System.Collections.Generic;
using System.IO;

namespace StarryEyes.Models.Plugins
{
    public static class PluginManager
    {
        private static Exception thrownException = null;
        internal static Exception ThrownException
        {
            get { return PluginManager.thrownException; }
        }

        private static object pluginsLocker = new object();
        private static List<IPlugin> plugins = new List<IPlugin>();

        public static IEnumerable<IPlugin> LoadedPlugins
        {
            get
            {
                lock (pluginsLocker)
                {
                    return plugins.AsReadOnly();
                }
            }
        }

        internal static void Load()
        {
            try
            {
            }
            catch (Exception ex)
            {
                thrownException = ex;
            }
        }

    }
}
