using System;

namespace StarryEyes.Mystique.Models.Plugins
{
    /// <summary>
    /// Interface for Krile StarryEyes
    /// </summary>
    public interface IPlugin
    {
        string Name { get; }

        Version Version { get; }

        void Initialize();
    }
}
