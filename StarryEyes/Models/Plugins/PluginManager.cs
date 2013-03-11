using System;
using System.Collections.Generic;

namespace StarryEyes.Models.Plugins
{
    public static class PluginManager
    {
        private static Exception _thrownException;
        internal static Exception ThrownException
        {
            get { return _thrownException; }
        }

        private static readonly object pluginsLocker = new object();
        private static readonly List<IPlugin> plugins = new List<IPlugin>();

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
                _thrownException = ex;
            }
        }

    }
}
