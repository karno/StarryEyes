using System;
using System.Windows.Controls;

namespace StarryEyes.Fragments.Plugins
{
    /// <summary>
    /// Interfaces of plugins for Krile StarryEyes
    /// </summary>
    public interface IPlugin
    {
        Guid Id { get; }

        string Name { get; }

        Version Version { get; }

        Uri SourceUri { get; }

        void Initialize();
    }

    /// <summary>
    /// Interface of plugins which supports configurations
    /// </summary>
    public interface IConfigurablePlugin : IPlugin
    {
        Control GetConfigurationPage();
    }
}
