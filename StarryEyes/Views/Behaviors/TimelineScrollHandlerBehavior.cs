using System.Windows.Controls;
using System.Windows.Interactivity;

namespace StarryEyes.Views.Behaviors
{
    public class TimelineScrollHandlerBehavior : Behavior<ScrollViewer>
    {
        protected override void OnAttached()
        {
            this.AssociatedObject.PreviewMouseWheel += AssociatedObject_PreviewMouseWheel;
            base.OnAttached();
        }

        void AssociatedObject_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            var newOffset = this.AssociatedObject.VerticalOffset - e.Delta;
            if (newOffset < 1)
            {
                // capture scroll event (for preventing freeze)
                this.AssociatedObject.ScrollToTop();
                e.Handled = true;
            }
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            this.AssociatedObject.PreviewMouseWheel -= AssociatedObject_PreviewMouseWheel;
        }
    }
}
