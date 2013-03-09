using System.Windows;

namespace StarryEyes.Views.Utils
{
    public sealed class NotNullToVisibleConverter : OneWayConverter<object, Visibility>
    {
        protected override Visibility ToTarget(object input, object parameter)
        {
            return input != null ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
