using System.Windows;
using System.Windows.Media;

namespace Sophia.Utilities
{
    public static class DependencyObjectHelper
    {
        public static T FindVisualChild<T>(this DependencyObject obj) where T : DependencyObject
        {
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                var child = VisualTreeHelper.GetChild(obj, i);
                if (child is T cTyped)
                {
                    return cTyped;
                }
                var descendant = FindVisualChild<T>(child);
                if (descendant != null)
                {
                    return descendant;
                }
            }
            return null;
        }
    }
}