using System;
using System.Windows.Controls;

namespace StarryEyes.Models.Plugins
{
    /// <summary>
    /// Interface for plugins works on Krile StarryEyes
    /// </summary>
    public interface IPlugin
    {
        string Name { get; }

        Version Version { get; }

        Uri SupportUri { get; }

        void Initialize();
    }

    /// <summary>
    /// Interface for configurable plugins
    /// </summary>
    public interface IConfigurablePlugin : IPlugin
    {
        Control GetConfigurationInterface();
    }
}
