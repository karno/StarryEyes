using System;

namespace StarryEyes.Models.Plugins
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
