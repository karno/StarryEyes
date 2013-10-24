using System.Windows;
using System.Windows.Media;

namespace StarryEyes.Views.Utils
{
    public static class DependencyObjectExtension
    {
        public static T FindVisualChild<T>(this DependencyObject obj) where T : DependencyObject
        {
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                var child = VisualTreeHelper.GetChild(obj, i);
                var cTyped = child as T;
                if (cTyped != null)
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
