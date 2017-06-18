using System.Windows;
using System.Windows.Interactivity;
using Sophia.Windows;

namespace Sophia.Behaviors
{
    public class HideFromTaskSwitcherBehavior : Behavior<Window>
    {
        private bool _isHide;

        protected override void OnAttached()
        {
            if (AssociatedObject.IsLoaded)
            {
                HideWindow(AssociatedObject);
                _isHide = true;
            }
            else
            {
                AssociatedObject.Loaded += AssociatedObjectOnLoaded;
            }
        }

        private void AssociatedObjectOnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            AssociatedObject.Loaded -= AssociatedObjectOnLoaded;
            HideWindow(AssociatedObject);
            _isHide = true;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.Loaded -= AssociatedObjectOnLoaded;
            if (_isHide)
            {
                UnhideWindow(AssociatedObject);
                _isHide = false;
            }
        }

        // ReSharper disable once InconsistentNaming
        private const int WS_EX_TOOLWINDOW = 0x0080;

        private static void HideWindow(Window window)
        {
            window.SetWindowExStyle(window.GetWindowExStyle() | WS_EX_TOOLWINDOW);
        }

        private static void UnhideWindow(Window window)
        {
            window.SetWindowExStyle(window.GetWindowExStyle() ^ WS_EX_TOOLWINDOW);
        }
    }
}