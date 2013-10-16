using System;
using System.Collections.Generic;
using StarryEyes.Feather;

namespace StarryEyes.Models.Plugins
{
    public static class PluginManager
    {
        private static Exception _thrownException;
        internal static Exception ThrownException
        {
            get { return _thrownException; }
        }

        private static readonly object _pluginsLocker = new object();
        private static readonly List<IPlugin> _plugins = new List<IPlugin>();

        public static IEnumerable<IPlugin> LoadedPlugins
        {
            get
            {
                lock (_pluginsLocker)
                {
                    return _plugins.AsReadOnly();
                }
            }
        }

        internal static void Load(string path)
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
