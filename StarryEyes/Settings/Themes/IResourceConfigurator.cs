using System.Windows;

namespace StarryEyes.Settings.Themes
{
    /// <summary>
    /// Interface for the class which can configurate XAML Resource dictionary
    /// </summary>
    public interface IResourceConfigurator
    {
        /// <summary>
        /// Configure resource dictionary
        /// </summary>
        /// <param name="dictionary">target resource dictionary</param>
        /// <param name="prefix">using prefix</param>
        void ConfigureResourceDictionary(ResourceDictionary dictionary, string prefix);
    }
}