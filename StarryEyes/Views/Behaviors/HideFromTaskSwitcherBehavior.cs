using System.Windows;
using System.Windows.Interactivity;
using StarryEyes.Nightmare.Windows;

namespace StarryEyes.Views.Behaviors
{
    public class HideFromTaskSwitcherBehavior : Behavior<Window>
    {
        private bool _isHide;

        protected override void OnAttached()
        {
            if (this.AssociatedObject.IsLoaded)
            {
                HideWindow(this.AssociatedObject);
                _isHide = true;
            }
            else
            {
                this.AssociatedObject.Loaded += AssociatedObjectOnLoaded;
            }
        }

        private void AssociatedObjectOnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            this.AssociatedObject.Loaded -= AssociatedObjectOnLoaded;
            HideWindow(this.AssociatedObject);
            _isHide = true;
        }

        protected override void OnDetaching()
        {
            this.AssociatedObject.Loaded -= AssociatedObjectOnLoaded;
            if (_isHide)
            {
                UnhideWindow(this.AssociatedObject);
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
