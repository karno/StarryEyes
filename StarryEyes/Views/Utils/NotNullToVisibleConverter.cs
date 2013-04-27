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

    public sealed class NotNullOrEmptyToVisibleConverter : OneWayConverter<string, Visibility>
    {
        protected override Visibility ToTarget(string input, object parameter)
        {
            return string.IsNullOrEmpty(input) ? Visibility.Collapsed : Visibility.Visible;
        }
    }
}
