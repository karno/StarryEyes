using System.Windows.Controls;
using System.Windows.Interactivity;
using StarryEyes.Settings;

namespace StarryEyes.Views.Behaviors
{
    public class TimelineScrollHandlerBehavior : Behavior<ScrollViewer>
    {
        protected override void OnAttached()
        {
            AssociatedObject.PreviewMouseWheel += AssociatedObject_PreviewMouseWheel;
            base.OnAttached();
        }

        void AssociatedObject_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            // this bugfix is available only on scroll by pixel setting.
            if (!Setting.IsScrollByPixel.Value) return;

            var newOffset = AssociatedObject.VerticalOffset - e.Delta;
            if (newOffset >= 1) return;

            // capture scroll event (for preventing freeze)
            AssociatedObject.ScrollToTop();
            e.Handled = true;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.PreviewMouseWheel -= AssociatedObject_PreviewMouseWheel;
        }
    }
}