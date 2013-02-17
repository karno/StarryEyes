using System.Windows;

namespace StarryEyes.Views.Utils
{
    public class WindowMaximizeToVisibleConverter : OneWayConverter<WindowState, Visibility>
    {
        protected override Visibility ToTarget(WindowState input, object parameter)
        {
            return input == WindowState.Maximized ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    public class WindowMaximizeToInvisibleConverter : OneWayConverter<WindowState, Visibility>
    {
        protected override Visibility ToTarget(WindowState input, object parameter)
        {
            return input != WindowState.Maximized ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    public class WindowMinimizeToVisibleConverter : OneWayConverter<WindowState, Visibility>
    {
        protected override Visibility ToTarget(WindowState input, object parameter)
        {
            return input == WindowState.Minimized ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    public class WindowMinimizeToInvisibleConverter : OneWayConverter<WindowState, Visibility>
    {
        protected override Visibility ToTarget(WindowState input, object parameter)
        {
            return input != WindowState.Minimized ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
