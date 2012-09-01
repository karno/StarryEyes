using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using StarryEyes.Mystique.Plugins;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition;
using StarryEyes.Mystique.Models.Hub;

namespace StarryEyes.Mystique.Models.Plugins
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
                var catalog = new DirectoryCatalog("plugins");
                var container = new CompositionContainer(catalog);
                var capsule = new PluginLoader();
                container.ComposeParts(capsule);
                if (capsule.Plugins != null)
                    plugins.AddRange(capsule.Plugins);
            }
            catch (Exception ex)
            {
                thrownException = ex;
            }
        }

        internal class PluginLoader
        {
            [ImportMany()]
            public List<IPlugin> Plugins = null;
        }
    }
}
