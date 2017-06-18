using System.ComponentModel;
using System.Windows;

namespace Sophia.Utilities
{
    internal static class DesignTimeHelper
    {
        internal static bool IsInDesignTime(DependencyObject dependencyObject)
        {
            return DesignerProperties.GetIsInDesignMode(dependencyObject);
        }

        internal static bool IsInDesignTime()
        {
            return IsInDesignTime(new DependencyObject());
        }
    }
}